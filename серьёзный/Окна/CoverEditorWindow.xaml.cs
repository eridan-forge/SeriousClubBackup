using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace серьёзный.Core.CoreVideo;

public partial class CoverEditorWindow : Window
{
    private readonly ScaleTransform scale = new(1, 1);

    private readonly TranslateTransform move = new();

    private Point start;

    private bool drag;

    public CoverEditorWindow(string file)
    {
        InitializeComponent();

        Image.Source =
            new BitmapImage(new Uri(file));

        var group = new TransformGroup();

        group.Children.Add(scale);
        group.Children.Add(move);

        Image.RenderTransform = group;

        Canvas.MouseWheel += Wheel;
        Canvas.MouseLeftButtonDown += Down;
        Canvas.MouseLeftButtonUp += Up;
        Canvas.MouseMove += Move;

        Save.Click += (_, _) =>
        {
            DialogResult = true;
        };
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
}