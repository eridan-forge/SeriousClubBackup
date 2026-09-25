using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreProfiles;

public class AchievementThresholds
{
    public long TenHoursSeconds { get; set; } = 10 * 3600;

    public int FiveFriendsCount { get; set; } = 5;

    public int VeteranSessionsCount { get; set; } = 50;
}

// Пороги встроенных достижений (10 часов / N друзей / N сеансов),
// вынесенные из констант в редактируемую таблицу. Список самих
// достижений (AchievementType) остаётся фиксированным в коде —
// это только числа, определяющие "когда" они выдаются.
public class AchievementThresholdsService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly AdminActionLogService лог = new();

    public AchievementThresholdsService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS AchievementThresholds(
            Id INTEGER PRIMARY KEY CHECK(Id=1),
            TenHoursSeconds INTEGER NOT NULL DEFAULT 36000,
            FiveFriendsCount INTEGER NOT NULL DEFAULT 5,
            VeteranSessionsCount INTEGER NOT NULL DEFAULT 50
        );

        INSERT OR IGNORE INTO AchievementThresholds
        VALUES(1, 36000, 5, 50);
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    public AchievementThresholds Get()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT TenHoursSeconds, FiveFriendsCount, VeteranSessionsCount " +
            "FROM AchievementThresholds WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return new AchievementThresholds();

        return new AchievementThresholds
        {
            TenHoursSeconds = r.GetInt64(0),
            FiveFriendsCount = r.GetInt32(1),
            VeteranSessionsCount = r.GetInt32(2)
        };
    }

    public void Save(AchievementThresholds thresholds, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE AchievementThresholds SET
            TenHoursSeconds=$h, FiveFriendsCount=$f, VeteranSessionsCount=$v
        WHERE Id=1;
        """;

        cmd.Parameters.AddWithValue("$h", thresholds.TenHoursSeconds);
        cmd.Parameters.AddWithValue("$f", thresholds.FiveFriendsCount);
        cmd.Parameters.AddWithValue("$v", thresholds.VeteranSessionsCount);

        cmd.ExecuteNonQuery();

        лог.Log(
            "Изменены пороги достижений",
            $"10ч={thresholds.TenHoursSeconds}с, друзья={thresholds.FiveFriendsCount}, ветеран={thresholds.VeteranSessionsCount} сеансов",
            adminName);
    }
}