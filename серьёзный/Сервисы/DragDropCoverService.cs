using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace серьёзный.Сервисы
{
    public static class DragDropCoverService
    {
        public static void Enable(
            Image image,
            System.Action<string> onImage)
        {
            image.AllowDrop = true;

            image.DragOver += (_, e) =>
            {
                e.Effects =
                    DragDropEffects.Copy;

                e.Handled = true;
            };

            image.Drop += (_, e) =>
            {
                if (!e.Data.GetDataPresent(
                    DataFormats.FileDrop))
                    return;

                var files =
                    (string[])e.Data.GetData(
                        DataFormats.FileDrop);

                foreach (var file in files)
                {
                    var ext =
                        Path.GetExtension(file)
                            .ToLower();

                    if (ext == ".png" ||
                        ext == ".jpg" ||
                        ext == ".jpeg" ||
                        ext == ".webp")
                    {
                        onImage(file);
                        break;
                    }
                }
            };
        }
    }
}