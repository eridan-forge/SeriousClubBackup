namespace серьёзный.Core.CoreShop;

public static class ShopSortService
{
    public static List<ShopItem> Sort(
        IEnumerable<ShopItem> items)
    {
        return items
            .OrderByDescending(x => x.Featured)
            .ThenByDescending(x => x.IsNew)
            .ThenBy(x => x.Name)
            .ToList();
    }
}