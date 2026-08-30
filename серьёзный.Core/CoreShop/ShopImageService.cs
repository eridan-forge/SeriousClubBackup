using System.IO;

namespace серьёзный.Core.CoreShop;

public static class ShopImageService
{
    public static string Save(string sourceFile)
    {
        ShopPaths.Ensure();

        var ext =
            Path.GetExtension(sourceFile);

        var name =
            $"{Guid.NewGuid()}{ext}";

        var destination =
            Path.Combine(
                ShopPaths.Images,
                name);

        File.Copy(
            sourceFile,
            destination,
            true);

        return destination;
    }

    public static void Delete(string file)
    {
        try
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        catch
        {
        }
    }
}