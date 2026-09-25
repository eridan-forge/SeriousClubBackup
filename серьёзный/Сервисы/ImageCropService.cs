using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace серьёзный.Сервисы
{
    public static class ImageCropService
    {
        public static void SaveCrop(
            BitmapSource source,
            string output)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(output)!);

            using var stream =
                File.Create(output);

            var encoder =
                new PngBitmapEncoder();

            encoder.Frames.Add(
                BitmapFrame.Create(source));

            encoder.Save(stream);
        }

        public static CroppedBitmap Crop(
            BitmapSource source,
            Int32Rect rect)
        {
            return new CroppedBitmap(
                source,
                rect);
        }
    }
}