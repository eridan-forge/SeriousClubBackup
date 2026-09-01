using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreAudit;
using System.IO;

namespace серьёзный.Core.CoreEconomy;

public class EconomyConfigService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly AdminActionLogService лог = new();

    public EconomyConfigService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS EconomyConfig(
            Id INTEGER PRIMARY KEY CHECK(Id=1),
            PointsPerMinutePurchased REAL NOT NULL DEFAULT 1.0,
            PointsPerAchievement INTEGER NOT NULL DEFAULT 50,
            PointsPerDrinkPurchase INTEGER NOT NULL DEFAULT 20,
            PremiumMultiplierBonusPercent REAL NOT NULL DEFAULT 20
        );

        INSERT OR IGNORE INTO EconomyConfig
        VALUES(1, 1.0, 50, 20, 20);

        CREATE TABLE IF NOT EXISTS LevelTiers(
            Level INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            MinPlayedSeconds INTEGER NOT NULL,
            MultiplierPercent REAL NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM LevelTiers;";

        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
        {
            var seed = new (int Level, string Name, long Sec, double Mult)[]
            {
                (1, "Новичок",   0,          100),
                (2, "Завсегдатай", 5 * 3600,  110),
                (3, "Ветеран",   20 * 3600,  125),
                (4, "Элита",     60 * 3600,  150),
                (5, "Легенда",   150 * 3600, 200)
            };

            foreach (var s in seed)
            {
                using var ins = con.CreateCommand();

                ins.CommandText =
                    "INSERT INTO LevelTiers VALUES($l,$n,$s,$m);";

                ins.Parameters.AddWithValue("$l", s.Level);
                ins.Parameters.AddWithValue("$n", s.Name);
                ins.Parameters.AddWithValue("$s", s.Sec);
                ins.Parameters.AddWithValue("$m", s.Mult);

                ins.ExecuteNonQuery();
            }
        }
    }

    private SqliteConnection Open()
    {
        var con = new SqliteConnection($"Data Source={db}");
        con.Open();
        return con;
    }

    // =====================================================
    // БАЗОВЫЕ СТАВКИ
    // =====================================================

    public EconomyConfig GetEconomy()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT PointsPerMinutePurchased, PointsPerAchievement, " +
            "PointsPerDrinkPurchase, PremiumMultiplierBonusPercent " +
            "FROM EconomyConfig WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        r.Read();

        return new EconomyConfig
        {
            PointsPerMinutePurchased = r.GetDouble(0),
            PointsPerAchievement = r.GetInt32(1),
            PointsPerDrinkPurchase = r.GetInt32(2),
            PremiumMultiplierBonusPercent = r.GetDouble(3)
        };
    }

    public void SaveEconomy(EconomyConfig config, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE EconomyConfig SET
            PointsPerMinutePurchased=$p,
            PointsPerAchievement=$a,
            PointsPerDrinkPurchase=$d,
            PremiumMultiplierBonusPercent=$pr
        WHERE Id=1;
        """;

        cmd.Parameters.AddWithValue("$p", config.PointsPerMinutePurchased);
        cmd.Parameters.AddWithValue("$a", config.PointsPerAchievement);
        cmd.Parameters.AddWithValue("$d", config.PointsPerDrinkPurchase);
        cmd.Parameters.AddWithValue("$pr", config.PremiumMultiplierBonusPercent);

        cmd.ExecuteNonQuery();

        лог.Log("Изменена экономика",
            $"баллы/мин={config.PointsPerMinutePurchased}, премиум-бонус={config.PremiumMultiplierBonusPercent}%",
            adminName);
    }

    // =====================================================
    // УРОВНИ
    // =====================================================

    public List<LevelTier> GetTiers()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Level, Name, MinPlayedSeconds, MultiplierPercent " +
            "FROM LevelTiers ORDER BY Level;";

        using var r = cmd.ExecuteReader();

        var list = new List<LevelTier>();

        while (r.Read())
        {
            list.Add(new LevelTier
            {
                Level = r.GetInt32(0),
                Name = r.GetString(1),
                MinPlayedSeconds = r.GetInt64(2),
                MultiplierPercent = r.GetDouble(3)
            });
        }

        return list;
    }

    public void SaveTier(LevelTier tier, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO LevelTiers(Level, Name, MinPlayedSeconds, MultiplierPercent)
        VALUES($l,$n,$s,$m)
        ON CONFLICT(Level) DO UPDATE SET
            Name=$n, MinPlayedSeconds=$s, MultiplierPercent=$m;
        """;

        cmd.Parameters.AddWithValue("$l", tier.Level);
        cmd.Parameters.AddWithValue("$n", tier.Name);
        cmd.Parameters.AddWithValue("$s", tier.MinPlayedSeconds);
        cmd.Parameters.AddWithValue("$m", tier.MultiplierPercent);

        cmd.ExecuteNonQuery();

        лог.Log("Изменён уровень",
            $"Уровень {tier.Level} «{tier.Name}»: x{tier.MultiplierPercent / 100.0:0.00} с {tier.MinPlayedSeconds}с",
            adminName);
    }

    public void DeleteTier(int level, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM LevelTiers WHERE Level=$l;";

        cmd.Parameters.AddWithValue("$l", level);

        cmd.ExecuteNonQuery();

        лог.Log("Удалён уровень", $"Уровень {level}", adminName);
    }
}