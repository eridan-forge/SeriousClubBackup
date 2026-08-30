using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreModels;
using System;
using System.IO;

namespace серьёзный.Core.CoreServices;

public class PlayerProfileService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public PlayerProfileService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS PlayerProfiles(
            Id TEXT PRIMARY KEY,
            Nickname TEXT NOT NULL,
            Avatar TEXT,
            Level INTEGER NOT NULL,
            Experience INTEGER NOT NULL,
            FriendsCount INTEGER NOT NULL,
            AchievementsCount INTEGER NOT NULL,
            Visits INTEGER NOT NULL,
            TotalPlayTime INTEGER NOT NULL,
            Frame TEXT,
            Theme TEXT
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

    public PlayerProfile Get(Guid id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT *
        FROM PlayerProfiles
        WHERE Id=$id;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            id.ToString());

        using var r = cmd.ExecuteReader();

        if (r.Read())
        {
            return new PlayerProfile
            {
                Id = Guid.Parse(r.GetString(0)),
                Nickname = r.GetString(1),
                Avatar = r.IsDBNull(2) ? "" : r.GetString(2),
                Level = r.GetInt32(3),
                Experience = r.GetInt32(4),
                FriendsCount = r.GetInt32(5),
                AchievementsCount = r.GetInt32(6),
                Visits = r.GetInt32(7),
                TotalPlayTime =
                    TimeSpan.FromSeconds(r.GetInt64(8)),
                Frame =
                    r.IsDBNull(9) ? "Default" : r.GetString(9),
                Theme =
                    r.IsDBNull(10) ? "Blue" : r.GetString(10)
            };
        }

        var profile =
            new PlayerProfile
            {
                Id = id
            };

        Save(profile);

        return profile;
    }

    public void Save(PlayerProfile p)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT OR REPLACE INTO PlayerProfiles
        VALUES(
        $id,
        $nick,
        $avatar,
        $lvl,
        $exp,
        $friends,
        $ach,
        $visits,
        $time,
        $frame,
        $theme);
        """;

        cmd.Parameters.AddWithValue("$id", p.Id.ToString());
        cmd.Parameters.AddWithValue("$nick", p.Nickname);
        cmd.Parameters.AddWithValue("$avatar", p.Avatar);
        cmd.Parameters.AddWithValue("$lvl", p.Level);
        cmd.Parameters.AddWithValue("$exp", p.Experience);
        cmd.Parameters.AddWithValue("$friends", p.FriendsCount);
        cmd.Parameters.AddWithValue("$ach", p.AchievementsCount);
        cmd.Parameters.AddWithValue("$visits", p.Visits);
        cmd.Parameters.AddWithValue(
            "$time",
            (long)p.TotalPlayTime.TotalSeconds);
        cmd.Parameters.AddWithValue("$frame", p.Frame);
        cmd.Parameters.AddWithValue("$theme", p.Theme);

        cmd.ExecuteNonQuery();
    }
}