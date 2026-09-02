using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using серьёзный.Core.CoreEvents;

namespace серьёзный.Core.CoreProfiles;

public enum AchievementType
{
    FirstLogin,
    TenHours,
    FiveFriends,
    FirstPurchase,
    Veteran
}

public class AchievementInfo
{
    public AchievementType Type { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public ProfileFrame? RewardFrame { get; set; }
}

public class AchievementService
{
    private readonly string db;

    public AchievementService()
    {
        var folder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub");

        Directory.CreateDirectory(folder);

        db = Path.Combine(folder, "SeriousClub.db");

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS PlayerAchievements(
            PlayerId TEXT NOT NULL,
            Achievement INTEGER NOT NULL,
            UnlockTime TEXT NOT NULL,
            PRIMARY KEY(PlayerId, Achievement)
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var con =
            new SqliteConnection($"Data Source={db}");

        con.Open();

        return con;
    }

    // ==========================================
    // ВСЕ ДОСТИЖЕНИЯ
    // ==========================================

    public List<AchievementInfo> All()
    {
        return new()
        {
            new()
            {
                Type = AchievementType.FirstLogin,
                Name = "Первый вход",
                Description = "Впервые вошёл в клуб.",
                RewardFrame = ProfileFrame.Silver
            },

            new()
            {
                Type = AchievementType.TenHours,
                Name = "10 часов",
                Description = "Провёл в клубе 10 часов.",
                RewardFrame = ProfileFrame.Gold
            },

            new()
            {
                Type = AchievementType.FiveFriends,
                Name = "Компания",
                Description = "Добавил 5 друзей.",
                RewardFrame = ProfileFrame.Neon
            },

            new()
            {
                Type = AchievementType.FirstPurchase,
                Name = "Первая покупка",
                Description = "Совершил первую покупку."
            },

            new()
            {
                Type = AchievementType.Veteran,
                Name = "Ветеран",
                Description = "Особое достижение.",
                RewardFrame = ProfileFrame.Legend
            }
        };
    }

    // ==========================================
    // СПИСОК ИГРОКА
    // ==========================================

    public List<AchievementType> Owned(Guid player)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT Achievement
        FROM PlayerAchievements
        WHERE PlayerId=$id
        ORDER BY Achievement;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        using var r = cmd.ExecuteReader();

        var list =
            new List<AchievementType>();

        while (r.Read())
        {
            list.Add(
                (AchievementType)r.GetInt32(0));
        }

        return list;
    }

    public bool Has(
        Guid player,
        AchievementType achievement)
    {
        return Owned(player)
            .Contains(achievement);
    }

    // ==========================================
    // ВЫДАТЬ ДОСТИЖЕНИЕ
    // ==========================================

    public bool Unlock(
        Guid player,
        AchievementType achievement)
    {
        if (Has(player, achievement))
            return false;

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO PlayerAchievements(
            PlayerId,
            Achievement,
            UnlockTime)
        VALUES(
            $id,
            $a,
            $time);
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        cmd.Parameters.AddWithValue(
            "$a",
            (int)achievement);

        cmd.Parameters.AddWithValue(
            "$time",
            DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        var reward =
            All().First(x => x.Type == achievement);

        if (reward.RewardFrame.HasValue)
        {
            var styles =
                new ProfileStyleService();

            styles.UnlockFrame(
                player,
                reward.RewardFrame.Value);
        }

        EventBus.Publish(
              new AchievementUnlockedEvent(
                   player,
                     reward.Name,
 reward.Description));

        return true;
    }

    // ==========================================
    // ИНФОРМАЦИЯ ДЛЯ ПРОФИЛЯ
    // ==========================================

    public List<(AchievementInfo Info, bool Unlocked)>
        ForProfile(Guid player)
    {
        var owned =
            Owned(player).ToHashSet();

        return All()
            .Select(x => (x, owned.Contains(x.Type)))
            .ToList();
    }
}