using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

// Тот же паттерн: локальная очередь "покажи мои заказы". Сервер
// отвечает JSON-списком (ShopOrdersDto), который здесь просто хранится
// как строка - список не помещается в скалярные колонки прежних мостов.
public static class ShopOrdersBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open() => серьёзный.Core.CoreDb.SqliteDb.Open();

    public static long CreateRequest(Guid accountId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO ShopOrdersRequests(AccountId, Done, Created) VALUES($a,0,$t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static (Guid AccountId, long Id)? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, AccountId FROM ShopOrdersRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return (Guid.Parse(r.GetString(1)), r.GetInt64(0));
    }

    public static void CompleteRequest(long id, ShopOrdersDto orders)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE ShopOrdersRequests SET Done=1, OrdersJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(orders));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static ShopOrdersDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Done, OrdersJson FROM ShopOrdersRequests WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<ShopOrdersDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM ShopOrdersRequests WHERE Done=1 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}