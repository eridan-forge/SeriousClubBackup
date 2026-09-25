using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace серьёзный.Окна
{
    public partial class ОкноРедактораОбложки : Window
    {
        private readonly BitmapImage image;

        private Point last;

        private bool dragging;

        private double scale = 1;

        public BitmapSource? Result { get; private set; }

        public ОкноРедактораОбложки(string path)
        {
            InitializeComponent();

            image =
                new BitmapImage(new Uri(path));

            ImageEditor.Source = image;

            // Стартовый масштаб — картинка сразу полностью закрывает
            // рамку 300×420 (как background-size: cover). Раньше
            // масштаб всегда был x1, и крупное фото открывалось
            // "приближенным" на маленький кусочек в углу — выглядело
            // так, будто редактор сломан.
            scale =
                Math.Max(
                    300.0 / image.PixelWidth,
                    420.0 / image.PixelHeight);

            ImageEditor.Width = image.PixelWidth * scale;
            ImageEditor.Height = image.PixelHeight * scale;

            // Центрируем сразу при открытии. Раньше Canvas.Left/Top
            // никогда явно не устанавливались, а их значение по
            // умолчанию в WPF — NaN, а не 0. Из-за этого первое же
            // перетаскивание считало NaN + число = NaN и оставалось
            // NaN навсегда — подвинуть картинку мышью было физически
            // невозможно, хотя обработчик мыши срабатывал исправно.
            Canvas.SetLeft(ImageEditor, (300 - ImageEditor.Width) / 2.0);
            Canvas.SetTop(ImageEditor, (420 - ImageEditor.Height) / 2.0);
        }

        private void Canvas_MouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            var центрX =
                Canvas.GetLeft(ImageEditor) + ImageEditor.Width / 2.0;

            var центрY =
                Canvas.GetTop(ImageEditor) + ImageEditor.Height / 2.0;

            scale += e.Delta > 0 ? 0.08 : -0.08;

            if (scale < 0.05)
                scale = 0.05;

            ImageEditor.Width =
                image.PixelWidth * scale;

            ImageEditor.Height =
                image.PixelHeight * scale;

            // Масштабируем от центра текущей позиции, а не от
            // левого верхнего угла — иначе при зуме картинка
            // "убегает" в сторону.
            Canvas.SetLeft(ImageEditor, центрX - ImageEditor.Width / 2.0);
            Canvas.SetTop(ImageEditor, центрY - ImageEditor.Height / 2.0);
        }

        private void Canvas_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            dragging = true;

            last = e.GetPosition(CanvasEditor);

            CanvasEditor.CaptureMouse();
        }

        private void Canvas_MouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            dragging = false;

            CanvasEditor.ReleaseMouseCapture();
        }

        private void Canvas_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!dragging)
                return;

            var p =
                e.GetPosition(CanvasEditor);

            Canvas.SetLeft(
                ImageEditor,
                Canvas.GetLeft(ImageEditor) + p.X - last.X);

            Canvas.SetTop(
                ImageEditor,
                Canvas.GetTop(ImageEditor) + p.Y - last.Y);

            last = p;
        }

        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Раньше здесь было "Result = image" — сохранялась исходная
            // нетронутая картинка целиком, перетаскивание и масштаб
            // ни на что не влияли. Теперь реально рендерим то, что
            // видно внутри рамки 300×420.
            var bmp =
                new RenderTargetBitmap(
                    300, 420, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(CanvasEditor);

            Result = bmp;

            DialogResult = true;
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}