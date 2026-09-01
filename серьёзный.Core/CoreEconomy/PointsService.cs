using Microsoft.Data.Sqlite;
using System.IO;

namespace серьёзный.Core.CoreEconomy;

public class PointsService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public PointsService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS PlayerPoints(
            PlayerId TEXT PRIMARY KEY,
            Points INTEGER NOT NULL DEFAULT 0,
            Premium INTEGER NOT NULL DEFAULT 0,
            PremiumUntil TEXT
        );

        CREATE TABLE IF NOT EXISTS PointsHistory(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PlayerId TEXT NOT NULL,
            Delta INTEGER NOT NULL,
            Reason TEXT NOT NULL DEFAULT '',
            AdminName TEXT,
            Time TEXT NOT NULL,
            BalanceAfter INTEGER NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var con = new SqliteConnection($"Data Source={db}");
        con.Open();
        return con;
    }

    public PlayerPoints Get(Guid playerId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Points, Premium, PremiumUntil FROM PlayerPoints WHERE PlayerId=$id;";

        cmd.Parameters.AddWithValue("$id", playerId.ToString());

        using var r = cmd.ExecuteReader();

        if (r.Read())
        {
            return new PlayerPoints
            {
                PlayerId = playerId,
                Points = r.GetInt64(0),
                Premium = r.GetInt32(1) == 1,
                PremiumUntil = r.IsDBNull(2) ? null : DateTime.Parse(r.GetString(2))
            };
        }

        r.Close();

        using var ins = con.CreateCommand();

        ins.CommandText =
            "INSERT INTO PlayerPoints(PlayerId, Points, Premium, PremiumUntil) " +
            "VALUES($id, 0, 0, NULL);";

        ins.Parameters.AddWithValue("$id", playerId.ToString());

        ins.ExecuteNonQuery();

        return new PlayerPoints { PlayerId = playerId, Points = 0 };
    }

    // Основной способ изменить баланс — всегда проходит через
    // историю, никаких "тихих" изменений баланса в обход лога.
    public long Award(Guid playerId, long delta, string reason, string? adminName = null)
    {
        var current = Get(playerId);

        var newBalance = current.Points + delta;

        if (newBalance < 0)
        {
            newBalance = 0;
            delta = -current.Points;
        }

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE PlayerPoints SET Points=$p WHERE PlayerId=$id;";

        cmd.Parameters.AddWithValue("$p", newBalance);
        cmd.Parameters.AddWithValue("$id", playerId.ToString());

        cmd.ExecuteNonQuery();

        using var hist = con.CreateCommand();

        hist.CommandText =
        """
        INSERT INTO PointsHistory(PlayerId, Delta, Reason, AdminName, Time, BalanceAfter)
        VALUES($id, $d, $r, $a, $t, $b);
        """;

        hist.Parameters.AddWithValue("$id", playerId.ToString());
        hist.Parameters.AddWithValue("$d", delta);
        hist.Parameters.AddWithValue("$r", reason);
        hist.Parameters.AddWithValue("$a", (object?)adminName ?? DBNull.Value);
        hist.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));
        hist.Parameters.AddWithValue("$b", newBalance);

        hist.ExecuteNonQuery();

        return newBalance;
    }

    public void SetExact(Guid playerId, long points, string reason, string? adminName = null)
    {
        var current = Get(playerId);

        Award(playerId, points - current.Points, reason, adminName);
    }

    public List<PointsHistoryEntry> GetHistory(Guid playerId, int take = 100)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Delta, Reason, AdminName, Time, BalanceAfter " +
            "FROM PointsHistory WHERE PlayerId=$id ORDER BY Id DESC LIMIT $take;";

        cmd.Parameters.AddWithValue("$id", playerId.ToString());
        cmd.Parameters.AddWithValue("$take", take);

        using var r = cmd.ExecuteReader();

        var list = new List<PointsHistoryEntry>();

        while (r.Read())
        {
            list.Add(new PointsHistoryEntry
            {
                Id = r.GetInt64(0),
                PlayerId = playerId,
                Delta = r.GetInt64(1),
                Reason = r.GetString(2),
                AdminName = r.IsDBNull(3) ? null : r.GetString(3),
                Time = DateTime.Parse(r.GetString(4)),
                BalanceAfter = r.GetInt64(5)
            });
        }

        return list;
    }
}