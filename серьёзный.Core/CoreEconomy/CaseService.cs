using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreAudit;
using System.IO;

namespace серьёзный.Core.CoreEconomy;

public class CaseService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly PointsService points = new();
    private readonly InventoryService inventory = new();
    private readonly AdminActionLogService лог = new();

    private static readonly Random random = new();

    public CaseService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Cases(
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Icon TEXT NOT NULL DEFAULT '📦',
            PriceInPoints INTEGER NOT NULL DEFAULT 100,
            Enabled INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS CaseRewards(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CaseId TEXT NOT NULL,
            Type TEXT NOT NULL,
            Value TEXT NOT NULL,
            Label TEXT NOT NULL DEFAULT '',
            Weight INTEGER NOT NULL DEFAULT 1
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var con = new SqliteConnection($"Data Source={db}");
        con.Open();
        return con;
    }

    // =====================================================
    // КЕЙСЫ (каталог)
    // =====================================================

    public List<CaseInfo> GetAll()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Name, Icon, PriceInPoints, Enabled FROM Cases ORDER BY PriceInPoints;";

        using var r = cmd.ExecuteReader();

        var list = new List<CaseInfo>();

        while (r.Read())
        {
            list.Add(new CaseInfo
            {
                Id = Guid.Parse(r.GetString(0)),
                Name = r.GetString(1),
                Icon = r.GetString(2),
                PriceInPoints = r.GetInt32(3),
                Enabled = r.GetInt32(4) == 1
            });
        }

        return list;
    }

    public void Save(CaseInfo info, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO Cases(Id, Name, Icon, PriceInPoints, Enabled)
        VALUES($id,$n,$i,$p,$e)
        ON CONFLICT(Id) DO UPDATE SET
            Name=$n, Icon=$i, PriceInPoints=$p, Enabled=$e;
        """;

        cmd.Parameters.AddWithValue("$id", info.Id.ToString());
        cmd.Parameters.AddWithValue("$n", info.Name);
        cmd.Parameters.AddWithValue("$i", info.Icon);
        cmd.Parameters.AddWithValue("$p", info.PriceInPoints);
        cmd.Parameters.AddWithValue("$e", info.Enabled ? 1 : 0);

        cmd.ExecuteNonQuery();

        лог.Log("Изменён кейс", $"«{info.Name}», цена {info.PriceInPoints}, вкл={info.Enabled}", adminName);
    }

    public void DeleteCase(Guid caseId, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM Cases WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", caseId.ToString());
        cmd.ExecuteNonQuery();

        using var cmd2 = con.CreateCommand();
        cmd2.CommandText = "DELETE FROM CaseRewards WHERE CaseId=$id;";
        cmd2.Parameters.AddWithValue("$id", caseId.ToString());
        cmd2.ExecuteNonQuery();

        лог.Log("Удалён кейс", caseId.ToString(), adminName);
    }

    // =====================================================
    // НАГРАДЫ КЕЙСА
    // =====================================================

    public List<CaseReward> GetRewards(Guid caseId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Type, Value, Label, Weight FROM CaseRewards WHERE CaseId=$id;";

        cmd.Parameters.AddWithValue("$id", caseId.ToString());

        using var r = cmd.ExecuteReader();

        var list = new List<CaseReward>();

        while (r.Read())
        {
            list.Add(new CaseReward
            {
                Id = r.GetInt64(0),
                CaseId = caseId,
                Type = Enum.Parse<CaseRewardType>(r.GetString(1)),
                Value = r.GetString(2),
                Label = r.GetString(3),
                Weight = r.GetInt32(4)
            });
        }

        return list;
    }

    public void SaveReward(CaseReward reward, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        if (reward.Id == 0)
        {
            cmd.CommandText =
                "INSERT INTO CaseRewards(CaseId, Type, Value, Label, Weight) " +
                "VALUES($c,$t,$v,$l,$w);";
        }
        else
        {
            cmd.CommandText =
                "UPDATE CaseRewards SET CaseId=$c, Type=$t, Value=$v, Label=$l, Weight=$w WHERE Id=$id;";

            cmd.Parameters.AddWithValue("$id", reward.Id);
        }

        cmd.Parameters.AddWithValue("$c", reward.CaseId.ToString());
        cmd.Parameters.AddWithValue("$t", reward.Type.ToString());
        cmd.Parameters.AddWithValue("$v", reward.Value);
        cmd.Parameters.AddWithValue("$l", reward.Label);
        cmd.Parameters.AddWithValue("$w", reward.Weight);

        cmd.ExecuteNonQuery();

        лог.Log("Изменена награда кейса", $"{reward.Label} (вес {reward.Weight})", adminName);
    }

    public void DeleteReward(long rewardId, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM CaseRewards WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", rewardId);
        cmd.ExecuteNonQuery();

        лог.Log("Удалена награда кейса", rewardId.ToString(), adminName);
    }

    // =====================================================
    // ОТКРЫТИЕ КЕЙСА
    // =====================================================

    public CaseOpenResult? Open(Guid playerId, Guid caseId, out string error)
    {
        error = "";

        var caseInfo = GetAll().FirstOrDefault(x => x.Id == caseId);

        if (caseInfo == null || !caseInfo.Enabled)
        {
            error = "Кейс недоступен.";
            return null;
        }

        var rewards = GetRewards(caseId);

        if (rewards.Count == 0)
        {
            error = "У кейса нет наград — обратитесь к администратору.";
            return null;
        }

        var balance = points.Get(playerId);

        if (balance.Points < caseInfo.PriceInPoints)
        {
            error = "Недостаточно баллов.";
            return null;
        }

        points.Award(playerId, -caseInfo.PriceInPoints, $"Открытие кейса «{caseInfo.Name}»");

        var totalWeight = rewards.Sum(x => x.Weight);

        var roll = random.Next(0, Math.Max(1, totalWeight));

        var cumulative = 0;

        var chosen = rewards[^1];

        foreach (var reward in rewards)
        {
            cumulative += reward.Weight;

            if (roll < cumulative)
            {
                chosen = reward;
                break;
            }
        }

        // Применяем награду, кроме TimeMinutes — время начисляет
        // вызывающая сторона (у неё есть доступ к СервисАккаунтов).
        switch (chosen.Type)
        {
            case CaseRewardType.Points:
                points.Award(playerId, long.Parse(chosen.Value), $"Награда кейса «{caseInfo.Name}»: {chosen.Label}");
                break;

            case CaseRewardType.Item:
                inventory.Grant(playerId, Guid.Parse(chosen.Value));
                break;
        }

        return new CaseOpenResult
        {
            Label = chosen.Label,
            Type = chosen.Type,
            Value = chosen.Value
        };
    }
}