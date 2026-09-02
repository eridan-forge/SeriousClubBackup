using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreAudit;

public class AdminActionLogEntry
{
    public long Id { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;

    public string? AdminName { get; set; }

    public string Action { get; set; } = "";

    public string Details { get; set; } = "";
}

public class AdminActionLogService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public AdminActionLogService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS AdminActionLog(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Time TEXT NOT NULL,
            AdminName TEXT,
            Action TEXT NOT NULL,
            Details TEXT NOT NULL DEFAULT ''
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    public void Log(string action, string details = "", string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO AdminActionLog(Time, AdminName, Action, Details)
        VALUES($t, $a, $act, $det);
        """;

        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$a", (object?)adminName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$act", action);
        cmd.Parameters.AddWithValue("$det", details);

        cmd.ExecuteNonQuery();
    }

    public List<AdminActionLogEntry> GetRecent(int take = 200)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Time, AdminName, Action, Details FROM AdminActionLog " +
            "ORDER BY Id DESC LIMIT $take;";

        cmd.Parameters.AddWithValue("$take", take);

        using var r = cmd.ExecuteReader();

        var list = new List<AdminActionLogEntry>();

        while (r.Read())
        {
            list.Add(new AdminActionLogEntry
            {
                Id = r.GetInt64(0),
                Time = DateTime.Parse(r.GetString(1)),
                AdminName = r.IsDBNull(2) ? null : r.GetString(2),
                Action = r.GetString(3),
                Details = r.GetString(4)
            });
        }

        return list;
    }
}