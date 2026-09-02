using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreShop;

namespace серьёзный.Core.CoreServices;

public class ShopPurchaseRequestRecord
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public int PcId { get; set; }

    public Guid ItemId { get; set; }

    public ShopDeliveryType Delivery { get; set; }
}

// Тот же паттерн, что AccountLoginBridgeService/AccountBalanceBridgeService:
// локальная очередь на ПК игрока. ЭкранКлуба кладёт запрос "купить товар",
// Патруль (тот же ПК) пересылает его на сервер, где ЕДИНСТВЕННЫЙ
// ShopRequestService реально создаёт заказ. Ответ (успех/ошибка/Id заказа)
// пишется обратно сюда же.
public static class ShopPurchaseBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        var con = new SqliteConnection($"Data Source={db}");
        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS ShopPurchaseRequests(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AccountId TEXT NOT NULL,
            PcId INTEGER NOT NULL,
            ItemId TEXT NOT NULL,
            Delivery TEXT NOT NULL,
            Status INTEGER NOT NULL DEFAULT 0,
            ResultRequestId TEXT,
            Error TEXT,
            Created TEXT NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();

        return con;
    }

    // Вызывает ОкноИгрока при нажатии "Хочу купить".
    public static long CreateRequest(
        Guid accountId, int pcId, Guid itemId, ShopDeliveryType delivery)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO ShopPurchaseRequests(AccountId, PcId, ItemId, Delivery, Status, Created) " +
            "VALUES($a,$pc,$i,$d,0,$t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$pc", pcId);
        cmd.Parameters.AddWithValue("$i", itemId.ToString());
        cmd.Parameters.AddWithValue("$d", delivery.ToString());
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    // Вызывает Патруль: берёт следующий необработанный запрос.
    public static ShopPurchaseRequestRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, AccountId, PcId, ItemId, Delivery FROM ShopPurchaseRequests " +
            "WHERE Status=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new ShopPurchaseRequestRecord
        {
            Id = r.GetInt64(0),
            AccountId = Guid.Parse(r.GetString(1)),
            PcId = r.GetInt32(2),
            ItemId = Guid.Parse(r.GetString(3)),
            Delivery = Enum.Parse<ShopDeliveryType>(r.GetString(4))
        };
    }

    public static void CompleteRequest(
        long id, bool success, Guid? resultRequestId, string? error)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE ShopPurchaseRequests
        SET Status=$s, ResultRequestId=$r, Error=$e
        WHERE Id=$id;
        """;

        cmd.Parameters.AddWithValue("$s", success ? 1 : 2);
        cmd.Parameters.AddWithValue("$r", (object?)resultRequestId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$e", (object?)error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    // Вызывает ОкноИгрока: опрашивает результат по Id.
    public static (int Status, Guid? RequestId, string? Error)? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Status, ResultRequestId, Error FROM ShopPurchaseRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return (
            r.GetInt32(0),
            r.IsDBNull(1) ? null : Guid.Parse(r.GetString(1)),
            r.IsDBNull(2) ? null : r.GetString(2));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM ShopPurchaseRequests WHERE Status<>0 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}