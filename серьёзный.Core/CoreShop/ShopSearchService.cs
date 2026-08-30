namespace серьёзный.Core.CoreShop;

public static class ShopSearchService
{
    public static List<ShopItem> Search(
        IEnumerable<ShopItem> items,
        string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return items.ToList();

        text = text.Trim();

        return items
            .Where(x =>
                x.Name.Contains(
                    text,
                    StringComparison.OrdinalIgnoreCase) ||

                x.Description.Contains(
                    text,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}