using Microsoft.Data.Sqlite;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

public static class PlayerChatHistoryBridgeService
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
                CREATE TABLE IF NOT EXISTS PlayerChatHistoryRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MeId TEXT NOT NULL,
                    FriendId TEXT NOT NULL,
                    Done INTEGER NOT NULL DEFAULT 0,
                    HistoryJson TEXT,
                    Created TEXT NOT NULL
                );
                """;

                cmd.ExecuteNonQuery();

                инициализировано = true;
            }
        }

        return con;
    }

    public static long CreateRequest(Guid me, Guid friend)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO PlayerChatHistoryRequests(MeId, FriendId, Done, Created) " +
            "VALUES($m,$f,0,$t);";

        cmd.Parameters.AddWithValue("$m", me.ToString());
        cmd.Parameters.AddWithValue("$f", friend.ToString());
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static (long Id, Guid Me, Guid Friend)? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, MeId, FriendId FROM PlayerChatHistoryRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return (r.GetInt64(0), Guid.Parse(r.GetString(1)), Guid.Parse(r.GetString(2)));
    }

    public static void CompleteRequest(long id, PlayerChatHistoryDto history)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE PlayerChatHistoryRequests SET Done=1, HistoryJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(history));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static PlayerChatHistoryDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Done, HistoryJson FROM PlayerChatHistoryRequests WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<PlayerChatHistoryDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM PlayerChatHistoryRequests WHERE Done=1 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}