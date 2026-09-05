using System;
using System.IO;

namespace серьёзный.Core.CoreServices;

// Живёт на СЕРВЕРЕ (админский ПК). Товары и обложки игр хранят
// абсолютный путь на диске админской машины — на любом другом
// физическом ПК он бессмыслен. Чтобы карточки реально показывали
// фото на клиентах, картинка (если не слишком большая) встраивается
// в сам ответ каталога как Base64 — тем же JSON, что уже летит по
// существующему bridge-каналу, без нового сетевого протокола.
public static class ImageEmbedHelper
{
    // Каталог товаров опрашивается раз в 5 секунд, игр — раз в 15,
    // и рассылается каждому ПК полностью заново (тот же принцип,
    // что и у остальных Bridge-сервисов проекта). 300 КБ с запасом
    // хватает на сжатую обложку/фото товара, но не раздувает один
    // ответ до неоправданных размеров даже при десятках карточек.
    public const long MaxEmbedBytes = 300 * 1024;

    public static (string? Data, string? Extension) TryEmbed(string? path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return (null, null);

            var info = new FileInfo(path);

            if (info.Length <= 0 || info.Length > MaxEmbedBytes)
                return (null, null);

            var bytes = File.ReadAllBytes(path);

            return (Convert.ToBase64String(bytes), Path.GetExtension(path));
        }
        catch
        {
            // Не должно ронять сборку каталога - в худшем случае
            // карточка останется без фото, как сейчас.
            return (null, null);
        }
    }
}