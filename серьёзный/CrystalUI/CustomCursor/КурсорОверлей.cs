using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace серьёзный.CrystalUI.CustomCursor;

// Единственный курсор на весь процесс приложения. Раньше он рисовался
// Canvas'ом внутри MainWindow — из-за этого он "замирал" при открытии
// любого другого окна и превращался в обычную стрелку над карточками ПК
// (там стоял явный Cursor=Hand, который всегда перебивает Window.Cursor
// родителя). CompositionTarget.Rendering синхронизирован с реальной
// частотой кадров композиции экрана, а не с частотой WPF-события
// MouseMove — отсюда настоящая, а не мнимая плавность.
public sealed class КурсорОверлей : Window
{
    private static КурсорОверлей? экземпляр;

    private readonly TranslateTransform сдвигГрадиента = new();
    private readonly DropShadowEffect свечение;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Win32Точка точка);

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Точка
    {
        public int X;
        public int Y;
    }

    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;

    private КурсорОверлей()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        Width = 40;
        Height = 40;
        IsHitTestVisible = false;
        Focusable = false;

        var корень = new Grid();

        var кистьГрадиент = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            RelativeTransform = сдвигГрадиента
        };

        кистьГрадиент.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x2E, 0x5C), 0));
        кистьГрадиент.GradientStops.Add(new GradientStop(Color.FromRgb(0x8A, 0x0F, 0x30), 0.5));
        кистьГрадиент.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x2E, 0x5C), 1));

        var данныеФигуры = Geometry.Parse(
            "M 0,0 L 0,28 L 6,22 L 11,32 L 15,30 L 10,21 L 19,21 Z");

        свечение = new DropShadowEffect
        {
            Color = Color.FromRgb(0xFF, 0x2E, 0x5C),
            BlurRadius = 14,
            ShadowDepth = 0,
            Opacity = 0.7
        };

        var фигура = new Path
        {
            Data = данныеФигуры,
            Stretch = Stretch.None,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Color.FromRgb(0x3A, 0x0A, 0x18)),
            Fill = кистьГрадиент,
            Effect = свечение
        };

        // Было Opacity=0.42 — на исходном курсоре карбон был почти не
        // виден. Здесь заметно плотнее.
        var карбон = new Path
        {
            Data = данныеФигуры,
            Stretch = Stretch.None,
            Fill = СоздатьКарбон(),
            Opacity = 0.85
        };

        корень.Children.Add(фигура);
        корень.Children.Add(карбон);

        Content = корень;

        ЗапуститьПереливГрадиента();

        Loaded += (_, _) => ОтключитьВзаимодействие();

        CompositionTarget.Rendering += СледитьЗаКурсором;
    }

    private static DrawingBrush СоздатьКарбон()
    {
        var группа = new DrawingGroup();

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0A)),
            null,
            new RectangleGeometry(new Rect(0, 0, 14, 14))));

        var светлые = new GeometryGroup();
        светлые.Children.Add(new RectangleGeometry(new Rect(0, 0, 7, 7)));
        светлые.Children.Add(new RectangleGeometry(new Rect(7, 7, 7, 7)));

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x16)), null, светлые));

        var тёмные = new GeometryGroup();
        тёмные.Children.Add(new RectangleGeometry(new Rect(7, 0, 7, 7)));
        тёмные.Children.Add(new RectangleGeometry(new Rect(0, 7, 7, 7)));

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), null, тёмные));

        return new DrawingBrush(группа)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 14, 14),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            RelativeTransform = new RotateTransform(45, 0.5, 0.5)
        };
    }

    // Тот же период (3.2с) и та же кривая (SineEase), что у градиента
    // текста "СЕРЬЁЗНЫЙ" в MainWindow.ЗапуститьПереливДляКисти — чтобы
    // оба переливались абсолютно синхронно и одинаково.
    private void ЗапуститьПереливГрадиента()
    {
        var сдвиг = new DoubleAnimation
        {
            From = -1,
            To = 1,
            Duration = TimeSpan.FromSeconds(3.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        сдвигГрадиента.BeginAnimation(TranslateTransform.XProperty, сдвиг);
    }

    private void СледитьЗаКурсором(object? sender, EventArgs e)
    {
        if (!GetCursorPos(out var точка))
            return;

        var источник = PresentationSource.FromVisual(this);

        if (источник?.CompositionTarget == null)
            return;

        var точкаDip =
            источник.CompositionTarget.TransformFromDevice.Transform(
                new Point(точка.X, точка.Y));

        Left = точкаDip.X;
        Top = точкаDip.Y;
    }

    private void ОтключитьВзаимодействие()
    {
        if (PresentationSource.FromVisual(this) is not HwndSource источник)
            return;

        var хендл = источник.Handle;
        var стиль = GetWindowLong(хендл, GWL_EXSTYLE);

        SetWindowLong(
            хендл,
            GWL_EXSTYLE,
            стиль | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    // Короткая цветная вспышка — раньше жила только внутри MainWindow
    // и переставала работать в любом другом окне.
    public static void Вспышка(Color цвет, double радиус = 24)
    {
        экземпляр?.ВспышкаВнутри(цвет, радиус);
    }

    private void ВспышкаВнутри(Color цвет, double радиус)
    {
        свечение.BeginAnimation(DropShadowEffect.ColorProperty, new ColorAnimation
        {
            To = цвет,
            Duration = TimeSpan.FromMilliseconds(90),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        });

        свечение.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation
        {
            To = радиус,
            Duration = TimeSpan.FromMilliseconds(90),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        });
    }

    public static void Запустить()
    {
        if (экземпляр != null)
            return;

        экземпляр = new КурсорОверлей();
        экземпляр.Show();
    }

    public static void Остановить()
    {
        if (экземпляр == null)
            return;

        CompositionTarget.Rendering -= экземпляр.СледитьЗаКурсором;
        экземпляр.Close();
        экземпляр = null;
    }
}