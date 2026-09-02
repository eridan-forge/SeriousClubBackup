using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreDb;

namespace серьёзный.Core.CoreShop;

public class ShopService
{
    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public ShopService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        ShopPaths.Ensure();

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS ShopCategories(
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            Hidden INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS ShopItems(
            Id TEXT PRIMARY KEY,
            CategoryId TEXT NOT NULL,
            Name TEXT NOT NULL,
            Description TEXT NOT NULL DEFAULT '',
            Price REAL NOT NULL DEFAULT 0,
            Image TEXT NOT NULL DEFAULT '',
            Hidden INTEGER NOT NULL DEFAULT 0,
            Featured INTEGER NOT NULL DEFAULT 0,
            IsNew INTEGER NOT NULL DEFAULT 0,
            Stock INTEGER NOT NULL DEFAULT -1
        );

        CREATE TABLE IF NOT EXISTS ShopSettings(
            Id INTEGER PRIMARY KEY CHECK(Id=1),
            Enabled INTEGER NOT NULL DEFAULT 1,
            ShowBanner INTEGER NOT NULL DEFAULT 1
        );

        INSERT OR IGNORE INTO ShopSettings VALUES(1, 1, 1);
        """;

        cmd.ExecuteNonQuery();

        МигрироватьИзJson();
    }
    private SqliteConnection Open() => SqliteDb.Open();

    // Одноразовый перенос старых данных из JSON (если они есть) в
    // SQLite, чтобы витрина/товары, накопленные до перехода, не
    // потерялись. Срабатывает только если таблица категорий пуста.
    private void МигрироватьИзJson()
    {
        using var con = Open();

        var check = con.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM ShopCategories;";

        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            return;

        if (!File.Exists(ShopPaths.Categories) && !File.Exists(ShopPaths.Items))
            return;

        try
        {
            if (File.Exists(ShopPaths.Categories))
            {
                var categories =
                    JsonSerializer.Deserialize<List<ShopCategory>>(
                        File.ReadAllText(ShopPaths.Categories)) ?? new();

                foreach (var c in categories)
                    ВставитьКатегорию(con, c);
            }

            if (File.Exists(ShopPaths.Items))
            {
                var items =
                    JsonSerializer.Deserialize<List<ShopItem>>(
                        File.ReadAllText(ShopPaths.Items)) ?? new();

                foreach (var i in items)
                    ВставитьТовар(con, i);
            }

            if (File.Exists(ShopPaths.Settings))
            {
                var settings =
                    JsonSerializer.Deserialize<ShopSettings>(
                        File.ReadAllText(ShopPaths.Settings));

                if (settings != null)
                    SaveSettings(settings);
            }
        }
        catch
        {
            // Повреждённые старые JSON-файлы не должны блокировать
            // запуск - продолжаем с тем, что успели прочитать.
        }
    }

    // =========================
    // НАСТРОЙКИ
    // =========================

    public ShopSettings GetSettings()
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT Enabled, ShowBanner FROM ShopSettings WHERE Id=1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return new ShopSettings();

        return new ShopSettings
        {
            Enabled = r.GetInt32(0) == 1,
            ShowBanner = r.GetInt32(1) == 1
        };
    }

    public void SaveSettings(ShopSettings settings)
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
            "UPDATE ShopSettings SET Enabled=$e, ShowBanner=$b WHERE Id=1;";

        cmd.Parameters.AddWithValue("$e", settings.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$b", settings.ShowBanner ? 1 : 0);

        cmd.ExecuteNonQuery();

        ShopChangedEvent.Notify();
    }

    // =========================
    // КАТЕГОРИИ
    // =========================

    public List<ShopCategory> GetCategories()
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT Id, Name, SortOrder, Hidden FROM ShopCategories ORDER BY SortOrder;";

        using var r = cmd.ExecuteReader();

        var list = new List<ShopCategory>();

        while (r.Read())
        {
            list.Add(new ShopCategory
            {
                Id = Guid.Parse(r.GetString(0)),
                Name = r.GetString(1),
                Order = r.GetInt32(2),
                Hidden = r.GetInt32(3) == 1
            });
        }

        return list;
    }

    public void AddCategory(ShopCategory category)
    {
        category.Order = GetCategories().Count;

        using var con = Open();

        ВставитьКатегорию(con, category);

        ShopChangedEvent.Notify();
    }

    private static void ВставитьКатегорию(SqliteConnection con, ShopCategory category)
    {
        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO ShopCategories(Id, Name, SortOrder, Hidden)
        VALUES($id,$name,$order,$hidden)
        ON CONFLICT(Id) DO UPDATE SET
            Name=$name, SortOrder=$order, Hidden=$hidden;
        """;

        cmd.Parameters.AddWithValue("$id", category.Id.ToString());
        cmd.Parameters.AddWithValue("$name", category.Name);
        cmd.Parameters.AddWithValue("$order", category.Order);
        cmd.Parameters.AddWithValue("$hidden", category.Hidden ? 1 : 0);

        cmd.ExecuteNonQuery();
    }

    public void RenameCategory(Guid id, string name)
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE ShopCategories SET Name=$n WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$id", id.ToString());

        cmd.ExecuteNonQuery();

        ShopChangedEvent.Notify();
    }

    public void DeleteCategory(Guid id)
    {
        using var con = Open();

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM ShopCategories WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM ShopItems WHERE CategoryId=$id;";
            cmd.Parameters.AddWithValue("$id", id.ToString());
            cmd.ExecuteNonQuery();
        }

        ShopChangedEvent.Notify();
    }

    public void MoveCategory(Guid id, int newIndex)
    {
        var list = GetCategories();

        var category = list.FirstOrDefault(x => x.Id == id);

        if (category == null)
            return;

        list.Remove(category);

        newIndex = Math.Clamp(newIndex, 0, list.Count);

        list.Insert(newIndex, category);

        using var con = Open();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Order = i;

            var cmd = con.CreateCommand();
            cmd.CommandText = "UPDATE ShopCategories SET SortOrder=$o WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$o", i);
            cmd.Parameters.AddWithValue("$id", list[i].Id.ToString());
            cmd.ExecuteNonQuery();
        }

        ShopChangedEvent.Notify();
    }

    // =========================
    // ТОВАРЫ
    // =========================

    public List<ShopItem> GetItems()
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText =
            "SELECT Id, CategoryId, Name, Description, Price, Image, Hidden, Featured, IsNew, Stock " +
            "FROM ShopItems;";

        using var r = cmd.ExecuteReader();

        var list = new List<ShopItem>();

        while (r.Read())
            list.Add(ПрочитатьТовар(r));

        return list;
    }

    public List<ShopItem> GetItems(Guid categoryId) =>
        GetItems().Where(x => x.CategoryId == categoryId).ToList();

    public List<ShopItem> GetItemsByCategory(Guid categoryId) =>
        GetItems().Where(x => x.CategoryId == categoryId && !x.Hidden).ToList();

    public List<ShopItem> GetVisibleItems() =>
        GetItems().Where(x => !x.Hidden).ToList();

    public void AddItem(ShopItem item)
    {
        using var con = Open();

        ВставитьТовар(con, item);

        ShopChangedEvent.Notify();
    }

    public void UpdateItem(ShopItem item)
    {
        using var con = Open();

        ВставитьТовар(con, item);

        ShopChangedEvent.Notify();
    }

    private static void ВставитьТовар(SqliteConnection con, ShopItem item)
    {
        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO ShopItems
        (Id, CategoryId, Name, Description, Price, Image, Hidden, Featured, IsNew, Stock)
        VALUES($id,$cat,$name,$desc,$price,$img,$hidden,$feat,$new,$stock)
        ON CONFLICT(Id) DO UPDATE SET
            CategoryId=$cat, Name=$name, Description=$desc, Price=$price,
            Image=$img, Hidden=$hidden, Featured=$feat, IsNew=$new, Stock=$stock;
        """;

        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$cat", item.CategoryId.ToString());
        cmd.Parameters.AddWithValue("$name", item.Name);
        cmd.Parameters.AddWithValue("$desc", item.Description);
        cmd.Parameters.AddWithValue("$price", item.Price);
        cmd.Parameters.AddWithValue("$img", item.Image);
        cmd.Parameters.AddWithValue("$hidden", item.Hidden ? 1 : 0);
        cmd.Parameters.AddWithValue("$feat", item.Featured ? 1 : 0);
        cmd.Parameters.AddWithValue("$new", item.IsNew ? 1 : 0);
        cmd.Parameters.AddWithValue("$stock", item.Stock);

        cmd.ExecuteNonQuery();
    }

    public void DeleteItem(Guid id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();
        cmd.CommandText = "DELETE FROM ShopItems WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();

        ShopChangedEvent.Notify();
    }

    private static ShopItem ПрочитатьТовар(SqliteDataReader r)
    {
        return new ShopItem
        {
            Id = Guid.Parse(r.GetString(0)),
            CategoryId = Guid.Parse(r.GetString(1)),
            Name = r.GetString(2),
            Description = r.GetString(3),
            Price = r.GetDecimal(4),
            Image = r.GetString(5),
            Hidden = r.GetInt32(6) == 1,
            Featured = r.GetInt32(7) == 1,
            IsNew = r.GetInt32(8) == 1,
            Stock = r.GetInt32(9)
        };
    }
}