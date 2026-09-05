using System;
using System.IO;

namespace серьёзный.Core.CoreServices;

// Живёт на КЛИЕНТСКОМ ПК (ЭкранКлуба). Принимает Base64-картинку из
// каталога товаров/игр и сохраняет её в локальный кэш на ЭТОМ
// физическом ПК, возвращая путь, который уже реально существует
// локально и годится для File.Exists/BitmapImage.
public static class ImageCacheService
{
    private static readonly string папка =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SeriousClub",
            "ImageCache");

    // cacheKey должен быть стабильным для одной и той же картинки
    // (например, Id товара/игры), чтобы не плодить копии на диске
    // при каждом обновлении каталога.
    public static string? SaveIfNeeded(
        string cacheKey,
        string? base64Data,
        string? extension)
    {
        if (string.IsNullOrWhiteSpace(base64Data) || string.IsNullOrWhiteSpace(extension))
            return null;

        try
        {
            Directory.CreateDirectory(папка);

            var безопасноеРасширение =
                extension.StartsWith(".", StringComparison.Ordinal)
                    ? extension
                    : "." + extension;

            var путь = Path.Combine(папка, cacheKey + безопасноеРасширение);

            byte[] байты;

            try
            {
                байты = Convert.FromBase64String(base64Data);
            }
            catch
            {
                return null;
            }

            if (!File.Exists(путь) || new FileInfo(путь).Length != байты.Length)
            {
                File.WriteAllBytes(путь, байты);
            }

            return путь;
        }
        catch
        {
            return null;
        }
    }
}