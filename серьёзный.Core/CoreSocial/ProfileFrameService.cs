using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace серьёзный.Core.CoreSocial;

public class ProfileFrame
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public class ProfileFrameService
{
    private const string Db = "serious.db";

    public ProfileFrameService()
    {
        using var db = new SqliteConnection($"Data Source={Db}");
        db.Open();

        new SqliteCommand(@"
CREATE TABLE IF NOT EXISTS ProfileFrames(
PlayerId TEXT PRIMARY KEY,
FrameId TEXT NOT NULL
);", db).ExecuteNonQuery();
    }

    public List<ProfileFrame> All()
    {
        return new()
        {
            new(){ Id="default", Name="Стандарт", Color="#64748B"},
            new(){ Id="silver", Name="Серебряная", Color="#C0C0C0"},
            new(){ Id="gold", Name="Золотая", Color="#F59E0B"},
            new(){ Id="neon", Name="Неоновая", Color="#22D3EE"},
            new(){ Id="ruby", Name="Рубиновая", Color="#DC2626"},
            new(){ Id="emerald", Name="Изумрудная", Color="#10B981"},
            new(){ Id="purple", Name="Фиолетовая", Color="#8B5CF6"}
        };
    }

    public string Get(Guid player)
    {
        using var db = new SqliteConnection($"Data Source={Db}");
        db.Open();

        var cmd = new SqliteCommand(
            "SELECT FrameId FROM ProfileFrames WHERE PlayerId=@id",
            db);

        cmd.Parameters.AddWithValue("@id", player.ToString());

        return cmd.ExecuteScalar()?.ToString() ?? "default";
    }

    public void Set(Guid player, string frame)
    {
        using var db = new SqliteConnection($"Data Source={Db}");
        db.Open();

        var cmd = new SqliteCommand(@"
INSERT INTO ProfileFrames(PlayerId,FrameId)
VALUES(@id,@frame)
ON CONFLICT(PlayerId)
DO UPDATE SET FrameId=@frame;", db);

        cmd.Parameters.AddWithValue("@id", player.ToString());
        cmd.Parameters.AddWithValue("@frame", frame);

        cmd.ExecuteNonQuery();
    }

    public ProfileFrame GetInfo(Guid player)
    {
        var id = Get(player);

        return All().Find(x => x.Id == id)
               ?? All()[0];
    }
}