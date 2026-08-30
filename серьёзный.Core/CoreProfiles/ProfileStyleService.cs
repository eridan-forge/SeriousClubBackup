using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace серьёзный.Core.CoreProfiles;

public enum ProfileFrame
{
    Default,
    Silver,
    Gold,
    Neon,
    Legend
}

public class ProfileStyle
{
    public Guid PlayerId { get; set; }

    public ProfileFrame Frame { get; set; } =
        ProfileFrame.Default;
}

public class ProfileStyleService
{
    private readonly string db;

    public ProfileStyleService()
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
        CREATE TABLE IF NOT EXISTS PlayerProfileStyles(
            PlayerId TEXT PRIMARY KEY,
            Frame INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS PlayerOwnedFrames(
            PlayerId TEXT NOT NULL,
            Frame INTEGER NOT NULL,
            PRIMARY KEY(PlayerId, Frame)
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
    // ТЕКУЩАЯ РАМКА
    // ==========================================

    public ProfileStyle Get(Guid player)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT Frame
        FROM PlayerProfileStyles
        WHERE PlayerId=$id;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        var value = cmd.ExecuteScalar();

        if (value == null)
        {
            EnsureDefault(player);

            return new ProfileStyle
            {
                PlayerId = player,
                Frame = ProfileFrame.Default
            };
        }

        return new ProfileStyle
        {
            PlayerId = player,
            Frame = (ProfileFrame)Convert.ToInt32(value)
        };
    }

    // ==========================================
    // СМЕНИТЬ РАМКУ
    // ==========================================

    public void SetFrame(
        Guid player,
        ProfileFrame frame)
    {
        if (!Owns(player, frame))
            return;

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO PlayerProfileStyles(
            PlayerId,
            Frame)
        VALUES(
            $id,
            $frame)
        ON CONFLICT(PlayerId)
        DO UPDATE SET
            Frame=$frame;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        cmd.Parameters.AddWithValue(
            "$frame",
            (int)frame);

        cmd.ExecuteNonQuery();
    }

    // ==========================================
    // ВЫДАТЬ РАМКУ
    // ==========================================

    public void UnlockFrame(
        Guid player,
        ProfileFrame frame)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT OR IGNORE INTO PlayerOwnedFrames(
            PlayerId,
            Frame)
        VALUES(
            $id,
            $frame);
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        cmd.Parameters.AddWithValue(
            "$frame",
            (int)frame);

        cmd.ExecuteNonQuery();
    }

    // ==========================================
    // СПИСОК РАМОК ИГРОКА
    // ==========================================

    public List<ProfileFrame> Owned(Guid player)
    {
        EnsureDefault(player);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT Frame
        FROM PlayerOwnedFrames
        WHERE PlayerId=$id
        ORDER BY Frame;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        using var r = cmd.ExecuteReader();

        var list =
            new List<ProfileFrame>();

        while (r.Read())
        {
            list.Add(
                (ProfileFrame)r.GetInt32(0));
        }

        return list;
    }

    public bool Owns(
        Guid player,
        ProfileFrame frame)
    {
        return Owned(player)
            .Contains(frame);
    }

    // ==========================================
    // ПЕРВАЯ РАМКА
    // ==========================================

    private void EnsureDefault(Guid player)
    {
        if (OwnsInternal(player))
            return;

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT OR IGNORE INTO PlayerOwnedFrames(
            PlayerId,
            Frame)
        VALUES(
            $id,
            0);
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        cmd.ExecuteNonQuery();
    }

    private bool OwnsInternal(Guid player)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT COUNT(*)
        FROM PlayerOwnedFrames
        WHERE PlayerId=$id;
        """;

        cmd.Parameters.AddWithValue(
            "$id",
            player.ToString());

        return Convert.ToInt32(
            cmd.ExecuteScalar()) > 0;
    }
}