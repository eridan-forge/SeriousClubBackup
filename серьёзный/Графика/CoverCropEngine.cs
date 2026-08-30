using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace серьёзный.Графика;

public static class CoverCropEngine
{
    public static void Save(
        FrameworkElement source,
        string file)
    {
        var bmp =
            new RenderTargetBitmap(
                300,
                420,
                96,
                96,
                System.Windows.Media.PixelFormats.Pbgra32);

        bmp.Render(source);

        var encoder =
            new PngBitmapEncoder();

        encoder.Frames.Add(
            BitmapFrame.Create(bmp));

        using var stream =
            File.Create(file);

        encoder.Save(stream);
    }
}