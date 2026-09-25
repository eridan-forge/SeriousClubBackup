using Microsoft.Data.Sqlite;

namespace серьёзный.Core.CoreServices;

public class PlayerChatOutboxRecord
{
    public long Id { get; set; }

    public Guid From { get; set; }

    public Guid To { get; set; }

    public string FromName { get; set; } = "";

    public string Text { get; set; } = "";
}

// Исходящее личное сообщение игрок->игрок: клиент кладёт сюда,
// Патруль этого же ПК шлёт на сервер, сервер пишет в общий ChatService
// (те же DirectMessages, что уже использует чат с админом).
public static class PlayerChatOutboxBridgeService
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
                CREATE TABLE IF NOT EXISTS PlayerChatOutbox(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FromId TEXT NOT NULL,
                    ToId TEXT NOT NULL,
                    FromName TEXT NOT NULL,
                    Text TEXT NOT NULL,
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

    public static void CreateRequest(Guid from, Guid to, string fromName, string text)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO PlayerChatOutbox(FromId, ToId, FromName, Text, Done, Created) " +
            "VALUES($f,$t,$n,$x,0,$c);";

        cmd.Parameters.AddWithValue("$f", from.ToString());
        cmd.Parameters.AddWithValue("$t", to.ToString());
        cmd.Parameters.AddWithValue("$n", fromName);
        cmd.Parameters.AddWithValue("$x", text);
        cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public static PlayerChatOutboxRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, FromId, ToId, FromName, Text FROM PlayerChatOutbox WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new PlayerChatOutboxRecord
        {
            Id = r.GetInt64(0),
            From = Guid.Parse(r.GetString(1)),
            To = Guid.Parse(r.GetString(2)),
            FromName = r.GetString(3),
            Text = r.GetString(4)
        };
    }

    public static void MarkDone(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE PlayerChatOutbox SET Done=1 WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM PlayerChatOutbox WHERE Done=1 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}