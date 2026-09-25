using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreEconomy;

public class CurrencyVisibility
{
    public bool ShowPoints { get; set; } = true;

    public bool ShowSessionCost { get; set; } = true;
}

// Что из "валют" показывать игроку в его окне — переключается админом
// одной галочкой, без пересборки. Новые валюты (сезонные и т.д.)
// добавляются сюда же новыми полями по тому же принципу.
public class CurrencyVisibilityService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public CurrencyVisibilityService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS CurrencyVisibility(
            Id INTEGER PRIMARY KEY CHECK(Id=1),
            ShowPoints INTEGER NOT NULL DEFAULT 1,
            ShowSessionCost INTEGER NOT NULL DEFAULT 1
        );

        INSERT OR IGNORE INTO CurrencyVisibility VALUES(1, 1, 1);
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    public CurrencyVisibility Get()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT ShowPoints, ShowSessionCost FROM CurrencyVisibility WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return new CurrencyVisibility();

        return new CurrencyVisibility
        {
            ShowPoints = r.GetInt32(0) == 1,
            ShowSessionCost = r.GetInt32(1) == 1
        };
    }

    public void Save(CurrencyVisibility visibility)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE CurrencyVisibility SET ShowPoints=$p, ShowSessionCost=$s WHERE Id=1;";

        cmd.Parameters.AddWithValue("$p", visibility.ShowPoints ? 1 : 0);
        cmd.Parameters.AddWithValue("$s", visibility.ShowSessionCost ? 1 : 0);

        cmd.ExecuteNonQuery();
    }
}