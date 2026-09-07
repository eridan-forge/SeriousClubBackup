using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace серьёзный.CrystalUI.CustomCursor;

// Единственный курсор на весь процесс приложения.
public sealed class КурсорОверлей : Window
{
    private static КурсорОверлей? экземпляр;

    private readonly TranslateTransform сдвигГрадиента = new();
    private readonly DropShadowEffect свечение;

    // Раньше фигура курсора начиналась ровно в (0,0) окна — свечению
    // некуда было выйти слева и сверху, там оно резко обрезалось.
    // Геометрия ниже сдвинута на Отступ по X и Y, вокруг неё есть поле
    // со всех четырёх сторон. Компенсируется в СледитьЗаКурсором, чтобы
    // кончик стрелки визуально совпадал с настоящим курсором Windows.
    private const double Отступ = 12;

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
        Width = 46;
        Height = 56;
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

        // Те же точки, что и раньше, просто сдвинутые на +12 по X и Y —
        // вокруг фигуры равномерное поле под свечение со всех сторон.
        var данныеФигуры = Geometry.Parse(
             "M 12,12 L 12,35.8 L 17.1,30.7 L 21.4,39.2 L 24.8,37.5 L 20.5,29.9 L 28.2,29.9 Z");

        // Свечение уменьшено и сделано менее заметным (было 14 / 0.7).
        свечение = new DropShadowEffect
        {
            Color = Color.FromRgb(0xFF, 0x2E, 0x5C),
            BlurRadius = 1,
            Opacity = 0.1
        };

        var фигура = new Path
        {
            Data = данныеФигуры,
            Stretch = Stretch.None,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Color.FromRgb(0x3A, 0x0A, 0x18)),
            Fill = кистьГрадиент,
            CacheMode = new BitmapCache()
        };

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

        таймерСлежения.Tick += СледитьЗаКурсором;

        Loaded += (_, _) =>
        {
            ОтключитьВзаимодействие();
            таймерСлежения.Start();
        };

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

        Left = точкаDip.X - Отступ;
        Top = точкаDip.Y - Отступ;
    }





    private static DrawingBrush СоздатьКарбон()
    {
        var группа = new DrawingGroup();

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x0A, 0x0A, 0x0A)),
            null,
            new RectangleGeometry(new Rect(0, 0, 6, 6))));

        var светлые = new GeometryGroup();
        светлые.Children.Add(new RectangleGeometry(new Rect(0, 0, 3, 3)));
        светлые.Children.Add(new RectangleGeometry(new Rect(3, 3, 3, 3)));

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x16)), null, светлые));

        var тёмные = new GeometryGroup();
        тёмные.Children.Add(new RectangleGeometry(new Rect(3, 0, 3, 3)));
        тёмные.Children.Add(new RectangleGeometry(new Rect(0, 3, 3, 3)));

        группа.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)), null, тёмные));

        // Тайл уменьшен с 14×14 до 6×6 — на маленькой фигуре курсора
        // помещалось всего 1-2 квадрата (текстура выглядела крупными
        // блоками), теперь плотность ближе к тому, как карбон выглядит
        // на большом фоне окна.
        return new DrawingBrush(группа)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, 6, 6),
            ViewportUnits = BrushMappingMode.Absolute,
            Stretch = Stretch.None,
            RelativeTransform = new RotateTransform(45, 0.5, 0.5)
        };
    }




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

    // Было: CompositionTarget.Rendering += СледитьЗаКурсором;
    // Стало: throttled DispatcherTimer вместо per-frame рендер-колбэка.

    private readonly DispatcherTimer таймерСлежения = new(DispatcherPriority.Input)
    {
        Interval = TimeSpan.FromMilliseconds(16) // ~60 fps глазу достаточно,
                                                 // но БЕЗ привязки к рендер-циклу
    };

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

    // Раньше вместе с цветом анимировался ещё и BlurRadius. Изменение
    // радиуса размытия у Effect на слоистом (WS_EX_LAYERED) Topmost-окне
    // каждый раз заставляет пересчитывать область отрисовки — поэтому
    // мигал не только курсор, а весь экран. Теперь радиус фиксирован,
    // анимируется только цвет.
    public static void Вспышка(Color цвет)
    {
        экземпляр?.ВспышкаВнутри(цвет);
    }

    private void ВспышкаВнутри(Color цвет)
    {
        свечение.BeginAnimation(DropShadowEffect.ColorProperty, new ColorAnimation
        {
            To = цвет,
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

        экземпляр.таймерСлежения.Stop();
        экземпляр.Close();
        экземпляр = null;
    }
}