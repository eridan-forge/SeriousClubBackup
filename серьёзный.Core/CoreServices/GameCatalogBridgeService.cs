using Microsoft.Data.Sqlite;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

// Тот же паттерн, что ChatHistoryBridgeService/ShopCatalogBridgeService:
// ЭкранКлуба просит список игр своего ПК, Патруль пересылает запрос на
// сервер, сервер отвечает списком из ЕДИНСТВЕННОГО СервисИгр (того же,
// что редактирует админ в "Настройка игр"), уже без скрытых игр.
public static class GameCatalogBridgeService
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
                CREATE TABLE IF NOT EXISTS GameCatalogRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PcId INTEGER NOT NULL,
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

    public static long CreateRequest(int pcId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO GameCatalogRequests(PcId, Done, Created) VALUES($pc,0,$t);";

        cmd.Parameters.AddWithValue("$pc", pcId);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();

        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public static (int PcId, long Id)? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, PcId FROM GameCatalogRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return (r.GetInt32(1), r.GetInt64(0));
    }

    public static void CompleteRequest(long id, GameCatalogDto catalog)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE GameCatalogRequests SET Done=1, CatalogJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(catalog));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static GameCatalogDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Done, CatalogJson FROM GameCatalogRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<GameCatalogDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM GameCatalogRequests WHERE Done=1 AND Created < $t;";

        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}