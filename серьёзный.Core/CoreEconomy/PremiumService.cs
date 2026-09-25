using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreEconomy;

public class PremiumService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly PointsService points = new(); // гарантирует наличие строки в PlayerPoints
    private readonly AdminActionLogService лог = new();

    private SqliteConnection Open() => SqliteDb.Open();

    public bool IsPremium(Guid playerId)
    {
        var p = points.Get(playerId);

        if (!p.Premium)
            return false;

        if (p.PremiumUntil.HasValue && p.PremiumUntil.Value < DateTime.Now)
            return false;

        return true;
    }

    public void SetPremium(Guid playerId, bool enabled, DateTime? until, string? adminName = null)
    {
        points.Get(playerId); // создаёт строку если нет

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE PlayerPoints SET Premium=$p, PremiumUntil=$u WHERE PlayerId=$id;";

        cmd.Parameters.AddWithValue("$p", enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$u", (object?)until?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", playerId.ToString());

        cmd.ExecuteNonQuery();

        лог.Log(
            enabled ? "Выдан премиум" : "Снят премиум",
            $"Игрок {playerId}, до {(until?.ToString("dd.MM.yyyy") ?? "бессрочно")}",
            adminName);
    }
}