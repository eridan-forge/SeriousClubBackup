using Microsoft.Data.Sqlite;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

// Тот же паттерн, что ShopOrdersBridgeService/ChatHistoryBridgeService:
// ЭкранКлуба просит витрину магазина, Патруль пересылает запрос на
// сервер (админку), сервер отвечает актуальным каталогом из ЕДИНСТВЕННОГО
// ShopService (того же, что редактирует админ в "Настройка магазина").
public static class ShopCatalogBridgeService
{
    private static bool инициализировано;
    private static readonly object блокировка = new();

    private static SqliteConnection Open()
    {
        var con = серьёзный.Core.CoreDb.SqliteDb.Open();

        lock (блокировка)
        {
            if (!инициализировано)
            {
                var cmd = con.CreateCommand();

                cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS ShopCatalogRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Done INTEGER NOT NULL DEFAULT 0,
                    CatalogJson TEXT,
                    Created TEXT NOT NULL
                );
                """;

                cmd.ExecuteNonQuery();

                инициализировано = true;
            }
        }

        return con;
    }

    public static long CreateRequest()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO ShopCatalogRequests(Done, Created) VALUES(0,$t);";

        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();

        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static long? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id FROM ShopCatalogRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        var value = cmd.ExecuteScalar();

        return value == null ? null : Convert.ToInt64(value);
    }

    public static void CompleteRequest(long id, ShopCatalogDto catalog)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE ShopCatalogRequests SET Done=1, CatalogJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(catalog));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static ShopCatalogDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Done, CatalogJson FROM ShopCatalogRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<ShopCatalogDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM ShopCatalogRequests WHERE Done=1 AND Created < $t;";

        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}