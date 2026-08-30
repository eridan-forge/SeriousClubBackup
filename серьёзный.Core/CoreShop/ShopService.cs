using System.Text.Json;
using System.IO;

namespace серьёзный.Core.CoreShop;

public class ShopService
{
    private readonly JsonSerializerOptions json =
        new()
        {
            WriteIndented = true
        };

    public ShopService()
    {
        ShopPaths.Ensure();
    }

    // =========================
    // НАСТРОЙКИ
    // =========================

    public ShopSettings GetSettings()
    {
        if (!File.Exists(ShopPaths.Settings))
            return new ShopSettings();

        try
        {
            return JsonSerializer.Deserialize<ShopSettings>(
                       File.ReadAllText(ShopPaths.Settings))
                   ?? new ShopSettings();
        }
        catch
        {
            return new ShopSettings();
        }
    }

    public void SaveSettings(ShopSettings settings)
    {
        File.WriteAllText(
            ShopPaths.Settings,
            JsonSerializer.Serialize(settings, json));

        ShopSyncService.Notify();
    }

    // =========================
    // КАТЕГОРИИ
    // =========================

    public List<ShopCategory> GetCategories()
    {
        if (!File.Exists(ShopPaths.Categories))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ShopCategory>>(
                       File.ReadAllText(ShopPaths.Categories))
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public ShopCategory CreateCategory(string name)
    {
        var list = GetCategories();

        var category =
            new ShopCategory
            {
                Name = name,
                Order = list.Count
            };

        list.Add(category);

        SaveCategories(list);

        return category;
    }

    public void RenameCategory(Guid id, string name)
    {
        var list = GetCategories();

        var category =
            list.FirstOrDefault(x => x.Id == id);

        if (category == null)
            return;

        category.Name = name;

        SaveCategories(list);
    }

    public void DeleteCategory(Guid id)
    {
        var categories = GetCategories();
        var items = GetItems();

        categories.RemoveAll(x => x.Id == id);
        items.RemoveAll(x => x.CategoryId == id);

        SaveCategories(categories);
        SaveItems(items);
    }

    public void MoveCategory(Guid id, int newIndex)
    {
        var list =
            GetCategories()
                .OrderBy(x => x.Order)
                .ToList();

        var category =
            list.FirstOrDefault(x => x.Id == id);

        if (category == null)
            return;

        list.Remove(category);

        newIndex =
            Math.Clamp(
                newIndex,
                0,
                list.Count);

        list.Insert(newIndex, category);

        for (int i = 0; i < list.Count; i++)
            list[i].Order = i;

        SaveCategories(list);
    }

    private void SaveCategories(List<ShopCategory> list)
    {
        File.WriteAllText(
            ShopPaths.Categories,
            JsonSerializer.Serialize(list, json));

        ShopSyncService.Notify();
    }

    // =========================
    // ТОВАРЫ
    // =========================

    public List<ShopItem> GetItems()
    {
        if (!File.Exists(ShopPaths.Items))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ShopItem>>(
                       File.ReadAllText(ShopPaths.Items))
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public ShopItem CreateItem(ShopItem item)
    {
        var list = GetItems();

        list.Add(item);

        SaveItems(list);

        return item;
    }

    public void UpdateItem(ShopItem item)
    {
        var list = GetItems();

        var current =
            list.FirstOrDefault(x => x.Id == item.Id);

        if (current == null)
            return;

        current.Name = item.Name;
        current.Description = item.Description;
        current.Price = item.Price;
        current.Stock = item.Stock;
        current.Hidden = item.Hidden;
        current.Featured = item.Featured;
        current.IsNew = item.IsNew;
        current.Image = item.Image;
        current.CategoryId = item.CategoryId;

        SaveItems(list);
    }

    public void DeleteItem(Guid id)
    {
        var list = GetItems();

        list.RemoveAll(x => x.Id == id);

        SaveItems(list);
    }

    private void SaveItems(List<ShopItem> list)
    {
        File.WriteAllText(
            ShopPaths.Items,
            JsonSerializer.Serialize(list, json));

        ShopSyncService.Notify();
    }

    // =========================
    // ЗАКАЗЫ
    // =========================

    public List<ShopOrder> GetOrders()
    {
        if (!File.Exists(ShopPaths.Orders))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ShopOrder>>(
                       File.ReadAllText(ShopPaths.Orders))
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void AddOrder(ShopOrder order)
    {
        var list = GetOrders();

        list.Add(order);

        SaveOrders(list);
    }

    public void CompleteOrder(Guid id)
    {
        var list = GetOrders();

        var order =
            list.FirstOrDefault(x => x.Id == id);

        if (order == null)
            return;

        order.Completed = true;

        SaveOrders(list);
    }

    private void SaveOrders(List<ShopOrder> list)
    {
        File.WriteAllText(
            ShopPaths.Orders,
            JsonSerializer.Serialize(list, json));

        ShopSyncService.Notify();
    }

    public List<ShopItem> GetItemsByCategory(Guid categoryId)
    {
        return GetItems()
            .Where(x =>
                x.CategoryId == categoryId &&
                !x.Hidden)
            .ToList();
    }

    public List<ShopItem> GetVisibleItems()
    {
        return GetItems()
            .Where(x => !x.Hidden)
            .ToList();
    }

    public ShopOrder CreateOrder(
        Guid itemId,
        Guid playerId,
        string playerName,
        string pcName)
    {
        var item =
            GetItems()
                .First(x => x.Id == itemId);

        var order =
            new ShopOrder
            {
                ItemId = itemId,
                PlayerId = playerId,
                PlayerName = playerName,
                PcName = pcName
            };

        AddOrder(order);

        ShopEvents.RaiseOrder(
            new ShopOrderNotification
            {
                OrderId = order.Id,
                PlayerId = playerId,
                PlayerName = playerName,
                PcName = pcName,
                ItemName = item.Name,
                Price = item.Price
            });

        return order;
    }

    public void AddCategory(ShopCategory category)
    {
        var list = GetCategories().ToList();

        list.Add(category);

        SaveCategories(list);
    }

    public void AddItem(ShopItem item)
    {
        var list = GetItems().ToList();

        list.Add(item);

        SaveItems(list);
    }

    public IEnumerable<ShopItem> GetItems(Guid categoryId)
    {
        return GetItems()
            .Where(x => x.CategoryId == categoryId);
    }


}