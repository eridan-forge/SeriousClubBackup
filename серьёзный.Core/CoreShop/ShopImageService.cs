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

    // Новый путь для сохранения уже отрендеренного (обрезанного через
    // CoverEditorWindow) изображения — без исходного файла для
    // копирования, поэтому результат всегда PNG.
    public static string NewPath()
    {
        ShopPaths.Ensure();

        return Path.Combine(
            ShopPaths.Images,
            $"{Guid.NewGuid()}.png");
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