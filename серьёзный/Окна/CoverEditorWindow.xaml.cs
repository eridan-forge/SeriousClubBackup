using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace серьёзный.Core.CoreVideo;

public partial class CoverEditorWindow : Window
{
    private readonly BitmapImage source;

    private readonly ScaleTransform scale = new(1, 1);

    private readonly TranslateTransform move = new();

    private Point start;

    private bool drag;

    public BitmapSource? Result { get; private set; }

    public CoverEditorWindow(string file)
    {
        InitializeComponent();

        source =
            new BitmapImage(new Uri(file));

        Image.Source = source;

        var group = new TransformGroup();

        group.Children.Add(scale);
        group.Children.Add(move);

        Image.RenderTransform = group;

        // Стартовый масштаб — картинка сразу полностью закрывает рамку
        // 300×420 (как background-size: cover), а не открывается в
        // исходном размере маленьким куском в углу.
        var начальныйМасштаб =
            Math.Max(
                300.0 / source.PixelWidth,
                420.0 / source.PixelHeight);

        scale.ScaleX = начальныйМасштаб;
        scale.ScaleY = начальныйМасштаб;

        move.X =
            (300 - source.PixelWidth * начальныйМасштаб) / 2.0;

        move.Y =
            (420 - source.PixelHeight * начальныйМасштаб) / 2.0;

        Canvas.MouseWheel += Wheel;
        Canvas.MouseLeftButtonDown += Down;
        Canvas.MouseLeftButtonUp += Up;
        Canvas.MouseMove += Move;

        Save.Click += Save_Click;
    }

    private void Wheel(object sender, MouseWheelEventArgs e)
    {
        double value =
            e.Delta > 0
                ? 1.08
                : 0.92;

        scale.ScaleX *= value;
        scale.ScaleY *= value;
    }

    private void Down(object sender, MouseButtonEventArgs e)
    {
        drag = true;
        start = e.GetPosition(Canvas);
        Canvas.CaptureMouse();
    }

    private void Up(object sender, MouseButtonEventArgs e)
    {
        drag = false;
        Canvas.ReleaseMouseCapture();
    }

    private void Move(object sender, MouseEventArgs e)
    {
        if (!drag)
            return;

        var p = e.GetPosition(Canvas);

        move.X += p.X - start.X;
        move.Y += p.Y - start.Y;

        start = p;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Раньше кнопка просто закрывала окно (DialogResult = true) —
        // класс вообще не имел свойства для результата. Вызывающий
        // код после закрытия копировал исходный файл целиком.
        var bmp =
            new RenderTargetBitmap(
                300, 420, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(Canvas);

        Result = bmp;

        DialogResult = true;
    }
}