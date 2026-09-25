using Microsoft.Data.Sqlite;
using серьёзный.Core.CoreAudit;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreEconomy;

public class InventoryService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private readonly PointsService points = new();
    private readonly AdminActionLogService лог = new();

    public InventoryService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS InventoryItems(
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Description TEXT NOT NULL DEFAULT '',
            Icon TEXT NOT NULL DEFAULT '🏆',
            PointsBonusPercent REAL NOT NULL DEFAULT 0,
            TimeBonusPercent REAL NOT NULL DEFAULT 0,
            PriceInPoints INTEGER NOT NULL DEFAULT 0,
            Enabled INTEGER NOT NULL DEFAULT 1
        );

        CREATE TABLE IF NOT EXISTS PlayerInventory(
            PlayerId TEXT NOT NULL,
            ItemId TEXT NOT NULL,
            Equipped INTEGER NOT NULL DEFAULT 0,
            AcquiredTime TEXT NOT NULL,
            PRIMARY KEY(PlayerId, ItemId)
        );
        """;

        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    // =====================================================
    // ПРЕДМЕТЫ (каталог, редактирует админ)
    // =====================================================

    public List<InventoryItem> GetAll()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Name, Description, Icon, PointsBonusPercent, " +
            "TimeBonusPercent, PriceInPoints, Enabled FROM InventoryItems ORDER BY Name;";

        using var r = cmd.ExecuteReader();

        var list = new List<InventoryItem>();

        while (r.Read())
        {
            list.Add(new InventoryItem
            {
                Id = Guid.Parse(r.GetString(0)),
                Name = r.GetString(1),
                Description = r.GetString(2),
                Icon = r.GetString(3),
                PointsBonusPercent = r.GetDouble(4),
                TimeBonusPercent = r.GetDouble(5),
                PriceInPoints = r.GetInt32(6),
                Enabled = r.GetInt32(7) == 1
            });
        }

        return list;
    }

    public void Save(InventoryItem item, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO InventoryItems
        (Id, Name, Description, Icon, PointsBonusPercent, TimeBonusPercent, PriceInPoints, Enabled)
        VALUES($id,$n,$d,$i,$pb,$tb,$price,$e)
        ON CONFLICT(Id) DO UPDATE SET
            Name=$n, Description=$d, Icon=$i,
            PointsBonusPercent=$pb, TimeBonusPercent=$tb,
            PriceInPoints=$price, Enabled=$e;
        """;

        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$n", item.Name);
        cmd.Parameters.AddWithValue("$d", item.Description);
        cmd.Parameters.AddWithValue("$i", item.Icon);
        cmd.Parameters.AddWithValue("$pb", item.PointsBonusPercent);
        cmd.Parameters.AddWithValue("$tb", item.TimeBonusPercent);
        cmd.Parameters.AddWithValue("$price", item.PriceInPoints);
        cmd.Parameters.AddWithValue("$e", item.Enabled ? 1 : 0);

        cmd.ExecuteNonQuery();

        лог.Log("Изменён предмет",
            $"«{item.Name}»: +{item.PointsBonusPercent}% баллов, +{item.TimeBonusPercent}% времени, цена {item.PriceInPoints}, вкл={item.Enabled}",
            adminName);
    }

    public void Delete(Guid itemId, string? adminName = null)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM InventoryItems WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", itemId.ToString());
        cmd.ExecuteNonQuery();

        using var cmd2 = con.CreateCommand();
        cmd2.CommandText = "DELETE FROM PlayerInventory WHERE ItemId=$id;";
        cmd2.Parameters.AddWithValue("$id", itemId.ToString());
        cmd2.ExecuteNonQuery();

        лог.Log("Удалён предмет", itemId.ToString(), adminName);
    }

    // =====================================================
    // ИНВЕНТАРЬ ИГРОКА
    // =====================================================

    public List<(InventoryItem Item, PlayerInventoryEntry Entry)> GetOwned(Guid playerId)
    {
        var allItems = GetAll().ToDictionary(x => x.Id);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT ItemId, Equipped, AcquiredTime FROM PlayerInventory WHERE PlayerId=$id;";

        cmd.Parameters.AddWithValue("$id", playerId.ToString());

        using var r = cmd.ExecuteReader();

        var result = new List<(InventoryItem, PlayerInventoryEntry)>();

        while (r.Read())
        {
            var itemId = Guid.Parse(r.GetString(0));

            if (!allItems.TryGetValue(itemId, out var item))
                continue;

            result.Add((item, new PlayerInventoryEntry
            {
                PlayerId = playerId,
                ItemId = itemId,
                Equipped = r.GetInt32(1) == 1,
                AcquiredTime = DateTime.Parse(r.GetString(2))
            }));
        }

        return result;
    }

    public bool Owns(Guid playerId, Guid itemId)
    {
        return GetOwned(playerId).Any(x => x.Item.Id == itemId);
    }

    public bool Buy(Guid playerId, Guid itemId, out string error)
    {
        error = "";

        var item = GetAll().FirstOrDefault(x => x.Id == itemId);

        if (item == null || !item.Enabled)
        {
            error = "Предмет недоступен.";
            return false;
        }

        if (Owns(playerId, itemId))
        {
            error = "Предмет уже есть в инвентаре.";
            return false;
        }

        var balance = points.Get(playerId);

        if (balance.Points < item.PriceInPoints)
        {
            error = "Недостаточно баллов.";
            return false;
        }

        points.Award(playerId, -item.PriceInPoints, $"Покупка предмета «{item.Name}»");

        Grant(playerId, itemId);

        return true;
    }

    public void Grant(Guid playerId, Guid itemId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT OR IGNORE INTO PlayerInventory(PlayerId, ItemId, Equipped, AcquiredTime) " +
            "VALUES($p,$i,0,$t);";

        cmd.Parameters.AddWithValue("$p", playerId.ToString());
        cmd.Parameters.AddWithValue("$i", itemId.ToString());
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public void SetEquipped(Guid playerId, Guid itemId, bool equipped)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE PlayerInventory SET Equipped=$e WHERE PlayerId=$p AND ItemId=$i;";

        cmd.Parameters.AddWithValue("$e", equipped ? 1 : 0);
        cmd.Parameters.AddWithValue("$p", playerId.ToString());
        cmd.Parameters.AddWithValue("$i", itemId.ToString());

        cmd.ExecuteNonQuery();
    }

    public InventoryBonus GetEquippedBonus(Guid playerId)
    {
        var owned = GetOwned(playerId).Where(x => x.Entry.Equipped && x.Item.Enabled);

        return new InventoryBonus
        {
            PointsBonusPercent = owned.Sum(x => x.Item.PointsBonusPercent),
            TimeBonusPercent = owned.Sum(x => x.Item.TimeBonusPercent)
        };
    }
}