using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreShop;

public class ShopRequestService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public ShopRequestService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS ShopRequests(
            Id TEXT PRIMARY KEY,
            AccountId TEXT NOT NULL,
            ItemId TEXT NOT NULL,
            ItemName TEXT NOT NULL,
            Price REAL NOT NULL,
            PcId INTEGER NOT NULL,
            Time TEXT NOT NULL,
            Status TEXT NOT NULL,
            Delivery TEXT NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();

        МигрироватьИзJson();
    }

    private SqliteConnection Open() => SqliteDb.Open();

    private void МигрироватьИзJson()
    {
        var oldFile = Path.Combine(ShopPaths.Root, "requests.json");

        using var con = Open();

        var check = con.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM ShopRequests;";

        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            return;

        if (!File.Exists(oldFile))
            return;

        try
        {
            var list =
                JsonSerializer.Deserialize<List<ShopRequest>>(File.ReadAllText(oldFile));

            if (list == null)
                return;

            foreach (var r in list)
                Сохранить(con, r);
        }
        catch
        {
        }
    }

    public IReadOnlyList<ShopRequest> All
    {
        get
        {
            using var con = Open();

            var cmd = con.CreateCommand();
            cmd.CommandText =
                "SELECT Id, AccountId, ItemId, ItemName, Price, PcId, Time, Status, Delivery " +
                "FROM ShopRequests ORDER BY Time;";

            using var r = cmd.ExecuteReader();

            var list = new List<ShopRequest>();

            while (r.Read())
                list.Add(Прочитать(r));

            return list;
        }
    }

    public ShopRequest Create(
        Guid accountId,
        int pcId,
        Guid itemId,
        string itemName,
        decimal price,
        ShopDeliveryType delivery)
    {
        var request = new ShopRequest
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PcId = pcId,
            ItemId = itemId,
            ItemName = itemName,
            Price = price,
            Delivery = delivery,
            Time = DateTime.Now,
            Status = ShopRequestStatus.Pending
        };

        using (var con = Open())
        {
            Сохранить(con, request);
        }

        ShopRequestEvent.Notify(request);

        // Раньше это не вызывалось нигде - счётчик заказов и окно
        // "Активные заказы" не обновлялись живьём при создании нового
        // заказа, только при смене статуса. Теперь оба пути одинаковы.
        ShopLiveEvents.NotifyCreated(request);

        return request;
    }

    public void SetPreparing(Guid id) => SetStatus(id, ShopRequestStatus.Preparing);

    public void SetReady(Guid id) => SetStatus(id, ShopRequestStatus.Ready);

    public void SetCompleted(Guid id) => SetStatus(id, ShopRequestStatus.Completed);

    public void Cancel(Guid id) => SetStatus(id, ShopRequestStatus.Cancelled);

    private void SetStatus(Guid id, ShopRequestStatus status)
    {
        ShopRequest? request;

        using (var con = Open())
        {
            var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE ShopRequests SET Status=$s WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$s", status.ToString());
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        request = All.FirstOrDefault(x => x.Id == id);

        if (request == null)
            return;

        ShopLiveEvents.NotifyUpdated(request);

        EventBus.Publish(new ShopOrderStatusChangedEvent(request.Id, status.ToString()));
    }

    private static void Сохранить(SqliteConnection con, ShopRequest r)
    {
        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO ShopRequests
        (Id, AccountId, ItemId, ItemName, Price, PcId, Time, Status, Delivery)
        VALUES($id,$acc,$item,$name,$price,$pc,$time,$status,$delivery)
        ON CONFLICT(Id) DO UPDATE SET
            AccountId=$acc, ItemId=$item, ItemName=$name, Price=$price,
            PcId=$pc, Time=$time, Status=$status, Delivery=$delivery;
        """;

        cmd.Parameters.AddWithValue("$id", r.Id.ToString());
        cmd.Parameters.AddWithValue("$acc", r.AccountId.ToString());
        cmd.Parameters.AddWithValue("$item", r.ItemId.ToString());
        cmd.Parameters.AddWithValue("$name", r.ItemName);
        cmd.Parameters.AddWithValue("$price", r.Price);
        cmd.Parameters.AddWithValue("$pc", r.PcId);
        cmd.Parameters.AddWithValue("$time", r.Time.ToString("O"));
        cmd.Parameters.AddWithValue("$status", r.Status.ToString());
        cmd.Parameters.AddWithValue("$delivery", r.Delivery.ToString());

        cmd.ExecuteNonQuery();
    }

    private static ShopRequest Прочитать(SqliteDataReader r)
    {
        return new ShopRequest
        {
            Id = Guid.Parse(r.GetString(0)),
            AccountId = Guid.Parse(r.GetString(1)),
            ItemId = Guid.Parse(r.GetString(2)),
            ItemName = r.GetString(3),
            Price = r.GetDecimal(4),
            PcId = r.GetInt32(5),
            Time = DateTime.Parse(r.GetString(6)),
            Status = Enum.Parse<ShopRequestStatus>(r.GetString(7)),
            Delivery = Enum.Parse<ShopDeliveryType>(r.GetString(8))
        };
    }
}