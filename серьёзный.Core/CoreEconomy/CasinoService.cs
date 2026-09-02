using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreAudit;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreEconomy;

public class CasinoService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly PointsService points = new();
    private readonly AdminActionLogService лог = new();

    private static readonly Random random = new();

    public CasinoService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS CasinoConfig(
            Id INTEGER PRIMARY KEY CHECK(Id=1),
            MinBet INTEGER NOT NULL DEFAULT 10,
            MaxBet INTEGER NOT NULL DEFAULT 500,
            WinChancePercent REAL NOT NULL DEFAULT 45,
            WinMultiplier REAL NOT NULL DEFAULT 1.8,
            Enabled INTEGER NOT NULL DEFAULT 1
        );

        INSERT OR IGNORE INTO CasinoConfig
        VALUES(1, 10, 500, 45, 1.8, 1);
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    public CasinoConfig GetConfig()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT MinBet, MaxBet, WinChancePercent, WinMultiplier, Enabled FROM CasinoConfig WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        r.Read();

        return new CasinoConfig
        {
            MinBet = r.GetInt32(0),
            MaxBet = r.GetInt32(1),
            WinChancePercent = r.GetDouble(2),
            WinMultiplier = r.GetDouble(3),
            Enabled = r.GetInt32(4) == 1
        };
    }

    public void SaveConfig(CasinoConfig config, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE CasinoConfig SET
            MinBet=$min, MaxBet=$max, WinChancePercent=$wc,
            WinMultiplier=$wm, Enabled=$e
        WHERE Id=1;
        """;

        cmd.Parameters.AddWithValue("$min", config.MinBet);
        cmd.Parameters.AddWithValue("$max", config.MaxBet);
        cmd.Parameters.AddWithValue("$wc", config.WinChancePercent);
        cmd.Parameters.AddWithValue("$wm", config.WinMultiplier);
        cmd.Parameters.AddWithValue("$e", config.Enabled ? 1 : 0);

        cmd.ExecuteNonQuery();

        лог.Log("Изменена настройка казино",
            $"шанс={config.WinChancePercent}%, множитель=x{config.WinMultiplier}, ставки {config.MinBet}-{config.MaxBet}",
            adminName);
    }

    public CasinoResult Play(Guid playerId, long bet, out string error)
    {
        error = "";

        var config = GetConfig();

        if (!config.Enabled)
        {
            error = "Казино сейчас отключено.";
            return new CasinoResult();
        }

        if (bet < config.MinBet || bet > config.MaxBet)
        {
            error = $"Ставка должна быть от {config.MinBet} до {config.MaxBet} баллов.";
            return new CasinoResult();
        }

        var balance = points.Get(playerId);

        if (balance.Points < bet)
        {
            error = "Недостаточно баллов.";
            return new CasinoResult();
        }

        points.Award(playerId, -bet, $"Казино: ставка {bet}");

        var win = random.NextDouble() * 100 < config.WinChancePercent;

        long payout = 0;

        if (win)
        {
            payout = (long)Math.Round(bet * config.WinMultiplier);

            points.Award(playerId, payout, $"Казино: выигрыш x{config.WinMultiplier:0.00}");
        }

        var finalBalance = points.Get(playerId);

        return new CasinoResult
        {
            Win = win,
            Bet = bet,
            Payout = payout,
            BalanceAfter = finalBalance.Points
        };
    }
}