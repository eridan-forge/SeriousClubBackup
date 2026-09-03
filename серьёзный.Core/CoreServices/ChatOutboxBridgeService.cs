using Microsoft.Data.Sqlite;

namespace серьёзный.Core.CoreServices;

public class ChatOutboxRequestRecord
{
    public long Id { get; set; }

    public int PcId { get; set; }

    public string Имя { get; set; } = "";

    public string Текст { get; set; } = "";
}

// Локальная очередь на ПК игрока: ОкноЛичногоЧата кладёт сюда исходящее
// сообщение, Патруль (тот же ПК) забирает его и шлёт по уже существующему
// сетевому каналу (ТипСообщения.Чат) — тем же, которым уже пользуется
// popup-ответ в ИсполнительКоманд. Ответа не ждём — доставка подтверждена
// самим фактом отправки в сокет.
public static class ChatOutboxBridgeService
{
    private static bool инициализировано;
    private static readonly object блокировка = new();

    private static SqliteConnection Open()
    {
        var con = серьёзный.Core.CoreDb.SqliteDb.Open();

        lock (блокировка)
        {
            if (!инициализировано)
            {
                var cmd = con.CreateCommand();

                cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS ChatOutboxRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PcId INTEGER NOT NULL,
                    Имя TEXT NOT NULL,
                    Текст TEXT NOT NULL,
                    Done INTEGER NOT NULL DEFAULT 0,
                    Created TEXT NOT NULL
                );
                """;

                cmd.ExecuteNonQuery();

                инициализировано = true;
            }
        }

        return con;
    }

    public static void CreateRequest(int pcId, string имя, string текст)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO ChatOutboxRequests(PcId, Имя, Текст, Done, Created) " +
            "VALUES($pc,$n,$t,0,$c);";

        cmd.Parameters.AddWithValue("$pc", pcId);
        cmd.Parameters.AddWithValue("$n", имя);
        cmd.Parameters.AddWithValue("$t", текст);
        cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public static ChatOutboxRequestRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, PcId, Имя, Текст FROM ChatOutboxRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new ChatOutboxRequestRecord
        {
            Id = r.GetInt64(0),
            PcId = r.GetInt32(1),
            Имя = r.GetString(2),
            Текст = r.GetString(3)
        };
    }

    public static void MarkDone(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE ChatOutboxRequests SET Done=1 WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM ChatOutboxRequests WHERE Done=1 AND Created < $t;";

        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}