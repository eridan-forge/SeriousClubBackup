using Microsoft.Data.Sqlite;
using System.IO;

namespace серьёзный.Core.CoreServices;

public class BalanceRequestRecord
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public bool Done { get; set; }

    public long RemainingSeconds { get; set; }

    public long PlayedSeconds { get; set; }

    public int SessionCount { get; set; }

    public bool Failed { get; set; }
}

public static class AccountBalanceBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open() => серьёзный.Core.CoreDb.SqliteDb.Open();

    public static long CreateRequest(Guid accountId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO AccountBalanceRequests(AccountId, Created) VALUES($a, $t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();

        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static BalanceRequestRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, AccountId FROM AccountBalanceRequests " +
            "WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new BalanceRequestRecord
        {
            Id = r.GetInt64(0),
            AccountId = Guid.Parse(r.GetString(1))
        };
    }

    public static void CompleteRequest(
        long id,
        bool success,
        long remainingSeconds,
        long playedSeconds,
        int sessionCount)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE AccountBalanceRequests
        SET Done=1, Failed=$f, RemainingSeconds=$r,
            PlayedSeconds=$p, SessionCount=$s
        WHERE Id=$id;
        """;

        cmd.Parameters.AddWithValue("$f", success ? 0 : 1);
        cmd.Parameters.AddWithValue("$r", remainingSeconds);
        cmd.Parameters.AddWithValue("$p", playedSeconds);
        cmd.Parameters.AddWithValue("$s", sessionCount);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static BalanceRequestRecord? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Done, Failed, RemainingSeconds, PlayedSeconds, SessionCount " +
            "FROM AccountBalanceRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new BalanceRequestRecord
        {
            Id = id,
            Done = r.GetInt32(0) == 1,
            Failed = r.GetInt32(1) == 1,
            RemainingSeconds = r.GetInt64(2),
            PlayedSeconds = r.GetInt64(3),
            SessionCount = r.GetInt32(4)
        };
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM AccountBalanceRequests WHERE Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}