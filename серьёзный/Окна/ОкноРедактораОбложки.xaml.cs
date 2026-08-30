using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        }

        private void Canvas_MouseWheel(
            object sender,
            MouseWheelEventArgs e)
        {
            scale += e.Delta > 0 ? 0.08 : -0.08;

            if (scale < 0.25)
                scale = 0.25;

            ImageEditor.Width =
                image.PixelWidth * scale;

            ImageEditor.Height =
                image.PixelHeight * scale;
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
            Result = image;

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