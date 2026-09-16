using Microsoft.Data.Sqlite;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices;

// Тот же паттерн, что ShopCatalogBridgeService: ЭкранКлуба просит
// погоду, Патруль пересылает запрос на сервер, сервер отвечает из
// своего кэша. Наружу (в интернет) с клиентского ПК не уходит ничего.
public static class WeatherBridgeService
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
                CREATE TABLE IF NOT EXISTS WeatherRequests(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Done INTEGER NOT NULL DEFAULT 0,
                    WeatherJson TEXT,
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

        // Чистим прямо здесь. Если Патруль на этом ПК не запущен,
        // его собственная уборка не отработает НИКОГДА, и запросы,
        // на которые никто не ответил, копились бы вечно. Удаляем
        // всё старше 2 часов независимо от статуса — просроченный
        // запрос погоды бесполезен в любом случае.
        using (var чистка = con.CreateCommand())
        {
            чистка.CommandText =
                "DELETE FROM WeatherRequests WHERE Created < $t;";

            чистка.Parameters.AddWithValue(
                "$t",
                (DateTime.Now - TimeSpan.FromHours(2)).ToString("O"));

            чистка.ExecuteNonQuery();
        }

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO WeatherRequests(Done, Created) VALUES(0,$t);";

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
            "SELECT Id FROM WeatherRequests WHERE Done=0 ORDER BY Id LIMIT 1;";

        var value = cmd.ExecuteScalar();

        return value == null ? null : Convert.ToInt64(value);
    }

    public static void CompleteRequest(long id, WeatherDto weather)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "UPDATE WeatherRequests SET Done=1, WeatherJson=$j WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$j", JsonSerializer.Serialize(weather));
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static WeatherDto? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "SELECT Done, WeatherJson FROM WeatherRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read() || r.GetInt32(0) == 0 || r.IsDBNull(1))
            return null;

        return JsonSerializer.Deserialize<WeatherDto>(r.GetString(1));
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM WeatherRequests WHERE Created < $t;";

        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}