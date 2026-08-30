using System.IO;

namespace серьёзный.Core.CoreShop;

public static class ShopPaths
{
    public static string Root =>
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.CommonApplicationData),
        "SeriousClub",
        "Shop");

    public static string Categories =>
        Path.Combine(Root, "categories.json");

    public static string Items =>
        Path.Combine(Root, "items.json");

    public static string Orders =>
        Path.Combine(Root, "orders.json");

    public static string Settings =>
        Path.Combine(Root, "settings.json");

    public static string Images =>
        Path.Combine(Root, "Images");

    public static void Ensure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Images);
    }
}