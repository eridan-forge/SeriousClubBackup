using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace серьёзный.Сервисы
{
    public static class DragDropGameService
    {
        public static void Enable(
            UIElement element,
            System.Action<string> onExe)
        {
            element.AllowDrop = true;

            element.DragOver += (_, e) =>
            {
                e.Effects =
                    DragDropEffects.Copy;

                e.Handled = true;
            };

            element.Drop += (_, e) =>
            {
                if (!e.Data.GetDataPresent(
                    DataFormats.FileDrop))
                    return;

                var files =
                    (string[])e.Data.GetData(
                        DataFormats.FileDrop);

                foreach (var file in files)
                {
                    if (Path.GetExtension(file)
                        .Equals(".exe",
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        onExe(file);
                    }
                }
            };
        }
    }
}