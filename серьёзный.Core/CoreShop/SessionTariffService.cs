using Microsoft.Data.Sqlite;
using System.IO;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreShop;

public class SessionTariff
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Minutes { get; set; }

    public decimal Price { get; set; }

    public string Label { get; set; } = "";

    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;
}

// Справочные тарифы сеансов — показываются игроку на "Главной" как
// "Тарифы". Редактирует админ (см. ПанельРазвлеченияАдмин, вкладка
// "Тарифы"). Сейчас это информация для игрока — саму продажу времени
// по-прежнему оформляет администратор.
public class SessionTariffService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public SessionTariffService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS SessionTariffs(
            Id TEXT PRIMARY KEY,
            Minutes INTEGER NOT NULL,
            Price REAL NOT NULL,
            Label TEXT NOT NULL DEFAULT '',
            SortOrder INTEGER NOT NULL DEFAULT 0,
            Enabled INTEGER NOT NULL DEFAULT 1
        );
        """;

        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM SessionTariffs;";

        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
        {
            var seed = new (int Minutes, decimal Price, string Label)[]
            {
                (30, 100m, "30 минут"),
                (60, 180m, "1 час"),
                (180, 480m, "3 часа"),
                (360, 850m, "Ночной (6 часов)")
            };

            int order = 0;

            foreach (var s in seed)
            {
                using var ins = con.CreateCommand();

                ins.CommandText =
                    "INSERT INTO SessionTariffs(Id, Minutes, Price, Label, SortOrder, Enabled) " +
                    "VALUES($id,$m,$p,$l,$o,1);";

                ins.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
                ins.Parameters.AddWithValue("$m", s.Minutes);
                ins.Parameters.AddWithValue("$p", (double)s.Price);
                ins.Parameters.AddWithValue("$l", s.Label);
                ins.Parameters.AddWithValue("$o", order++);

                ins.ExecuteNonQuery();
            }
        }
    }

    private SqliteConnection Open() => SqliteDb.Open();

    public List<SessionTariff> GetAll()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Minutes, Price, Label, SortOrder, Enabled FROM SessionTariffs ORDER BY SortOrder;";

        using var r = cmd.ExecuteReader();

        var list = new List<SessionTariff>();

        while (r.Read())
        {
            list.Add(new SessionTariff
            {
                Id = Guid.Parse(r.GetString(0)),
                Minutes = r.GetInt32(1),
                Price = (decimal)r.GetDouble(2),
                Label = r.GetString(3),
                SortOrder = r.GetInt32(4),
                Enabled = r.GetInt32(5) == 1
            });
        }

        return list;
    }

    public void Save(SessionTariff tariff)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO SessionTariffs(Id, Minutes, Price, Label, SortOrder, Enabled)
        VALUES($id,$m,$p,$l,$o,$e)
        ON CONFLICT(Id) DO UPDATE SET
            Minutes=$m, Price=$p, Label=$l, SortOrder=$o, Enabled=$e;
        """;

        cmd.Parameters.AddWithValue("$id", tariff.Id.ToString());
        cmd.Parameters.AddWithValue("$m", tariff.Minutes);
        cmd.Parameters.AddWithValue("$p", (double)tariff.Price);
        cmd.Parameters.AddWithValue("$l", tariff.Label);
        cmd.Parameters.AddWithValue("$o", tariff.SortOrder);
        cmd.Parameters.AddWithValue("$e", tariff.Enabled ? 1 : 0);

        cmd.ExecuteNonQuery();
    }

    public void Delete(Guid id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM SessionTariffs WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
    }
}