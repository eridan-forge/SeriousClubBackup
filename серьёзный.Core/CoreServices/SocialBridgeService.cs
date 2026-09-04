using Microsoft.Data.Sqlite;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

// Тот же паттерн, что EconomyBridgeService: игрок кладёт запрос (просто
// "покажи состояние" или конкретное действие с друзьями), Патруль
// пересылает на сервер, сервер строит состояние из ЕДИНСТВЕННОГО
// SocialService + активных сеансов и отвечает сюда же.
public static class SocialBridgeService
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
                CREATE TABLE IF NOT EXISTS SocialRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccountId TEXT NOT NULL,
                    ActionJson TEXT NOT NULL,
                    Status INTEGER NOT NULL DEFAULT 0,
                    ResultJson TEXT,
                    Created TEXT NOT NULL
                );
                """;

                cmd.ExecuteNonQuery();

                инициализировано = true;
            }
        }

        return con;
    }

    public static long CreateRequest(Guid accountId, SocialActionDto action)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO SocialRequests(AccountId, ActionJson, Status, Created) " +
            "VALUES($a,$j,0,$t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(action));
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static (long Id, Guid AccountId, SocialActionDto Action)? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, AccountId, ActionJson FROM SocialRequests WHERE Status=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        var action = JsonSerializer.Deserialize<SocialActionDto>(r.GetString(2));

        if (action == null)
            return null;

        return (r.GetInt64(0), Guid.Parse(r.GetString(1)), action);
    }

    public static void CompleteRequest(long id, SocialStateDto result)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE SocialRequests SET Status=1, ResultJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(result));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static SocialStateDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Status, ResultJson FROM SocialRequests WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<SocialStateDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM SocialRequests WHERE Status=1 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}