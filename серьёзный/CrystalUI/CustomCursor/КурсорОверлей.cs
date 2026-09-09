using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace серьёзный.CrystalUI.CustomCursor;

// =====================================================================
// АРХИТЕКТУРА (важно, если будешь трогать этот файл):
//
// Раньше окно курсора создавалось на ГЛАВНОМ потоке приложения (том же,
// где живёт MainWindow), и что таймер, что CompositionTarget.Rendering
// физически стоят в ОДНОЙ очереди сообщений с MainWindow. Пока главное
// окно занято отрисовкой (перелив градиентов, эффекты) — обновление
// позиции курсора становится в ту же очередь и не может выполниться
// быстрее, чем главный поток освобождается.
//
// Предыдущая попытка "оптимизации" (DispatcherTimer на приоритете
// Input) сделала строго хуже: приоритет Input НИЖЕ приоритета Render,
// на котором крутятся все перманентные анимации MainWindow — тик
// таймера вставал в очередь ПОСЛЕ всей отрисовки и откладывался
// произвольно долго.
//
// РЕШЕНИЕ: окно курсора живёт на ОТДЕЛЬНОМ потоке со своим собственным
// Dispatcher'ом — у него нет соседей по очереди, MainWindow физически
// не может встать перед обновлением позиции курсора, потому что это
// другая очередь сообщений Windows. Даже если главный поток встанет
// колом на секунду — курсор всё равно двигается идеально плавно.
//
// Позиция окна ставится напрямую через Win32 SetWindowPos, а не через
// Window.Left/Top — это полностью пропускает систему разметки WPF
// (Measure/Arrange), которая тут просто не нужна: окну не надо ничего
// пересчитывать, только физически сдвинуться.
// =====================================================================

public sealed class КурсорОверлей : Window
{
    private static КурсорОверлей? экземпляр;
    private static Thread? потокКурсора;
    private static Dispatcher? диспетчерКурсора;

    private readonly TranslateTransform сдвигГрадиента = new();
    private readonly DropShadowEffect свечение;

    private const double Отступ = 12;

    private IntPtr хендл;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Win32Точка точка);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

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

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOREDRAW = 0x0008;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

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

        var данныеФигуры = Geometry.Parse(
             "M 12,12 L 12,35.8 L 17.1,30.7 L 21.4,39.2 L 24.8,37.5 L 20.5,29.9 L 28.2,29.9 Z");

        // Геометрия никогда не меняется — замораживаем, чтобы WPF не
        // тратил время на отслеживание изменений там, где их не будет.
        данныеФигуры.Freeze();

        свечение = new DropShadowEffect
        {
            Color = Color.FromRgb(0xFF, 0x2E, 0x5C),
            BlurRadius = 1,
            Opacity = 0.1
        };

        var штрихКисть = new SolidColorBrush(Color.FromRgb(0x3A, 0x0A, 0x18));
        штрихКисть.Freeze();

        var фигура = new Path
        {
            Data = данныеФигуры,
            Stretch = Stretch.None,
            StrokeThickness = 1,
            Stroke = штрихКисть,
            Fill = кистьГрадиент,

            // РАНЬШЕ свечение создавалось и анимировалось в Вспышка(),
            // но никогда не подключалось сюда через Effect — то есть
            // вспышка при клике физически не была видна. Теперь видна.
            Effect = свечение,

            CacheMode = new BitmapCache()
        };

        var карбон = new Path
        {
            Data = данныеФигуры,
            Stretch = Stretch.None,
            Fill = СоздатьКарбон(),
            Opacity = 0.85,
            CacheMode = new BitmapCache()
        };

        корень.Children.Add(фигура);
        корень.Children.Add(карбон);

        Content = корень;

        ЗапуститьПереливГрадиента();

        SourceInitialized += (_, _) =>
        {
            хендл = new WindowInteropHelper(this).Handle;
            ОтключитьВзаимодействие();
        };

        Loaded += (_, _) =>
        {
            // Тикает синхронно с фактически отрисовываемыми кадрами
            // ЭТОГО потока — а не главного. Работает надёжно, потому
            // что перелив градиента (ЗапуститьПереливГрадиента) крутится
            // вечно, а значит здесь всегда есть что рендерить, и это
            // событие никогда не "засыпает".
            CompositionTarget.Rendering += СледитьЗаКурсором;
        };
    }

    private int последнийX = int.MinValue;
    private int последнийY = int.MinValue;

    private void СледитьЗаКурсором(object? sender, EventArgs e)
    {
        if (хендл == IntPtr.Zero)
            return;

        if (!GetCursorPos(out var точка))
            return;

        // ВАЖНО: GetCursorPos и SetWindowPos работают в ОДНОМ и том же
        // пространстве координат (физические пиксели экрана) — в
        // отличие от Window.Left/Top, которые всегда ждут логические
        // DIP-пиксели. Поэтому здесь НЕТ пересчёта DIP <-> устройство
        // для самой позиции — только для маленького отступа Отступ,
        // чтобы кончик стрелки одинаково совпадал с курсором что на
        // обычном мониторе, что при масштабе 125%/150%/200%.
        var источник = PresentationSource.FromVisual(this);

        var масштаб = источник?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

        var отступPx = (int)Math.Round(Отступ * масштаб);

        var целX = точка.X - отступPx;
        var целY = точка.Y - отступPx;

        // Мышь не двигалась с прошлого кадра — незачем гонять DWM.
        if (целX == последнийX && целY == последнийY)
            return;

        последнийX = целX;
        последнийY = целY;




        SetWindowPos(
            хендл,
            IntPtr.Zero,
             целX,
               целY,
            0, 0,
            SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOREDRAW | SWP_ASYNCWINDOWPOS);
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

        группа.Freeze();

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

    private void ОтключитьВзаимодействие()
    {
        if (хендл == IntPtr.Zero)
            return;

        var стиль = GetWindowLong(хендл, GWL_EXSTYLE);

        SetWindowLong(
            хендл,
            GWL_EXSTYLE,
            стиль | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    public static void Вспышка(Color цвет)
    {
        // Курсор живёт на другом потоке — сюда нельзя просто взять и
        // дёрнуть его свойство, нужно передать команду через его
        // собственный диспетчер.
        диспетчерКурсора?.BeginInvoke(
            DispatcherPriority.Send,
            new Action(() => экземпляр?.ВспышкаВнутри(цвет)));
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
        if (потокКурсора != null)
            return;

        var готово = new ManualResetEventSlim(false);

        потокКурсора = new Thread(() =>
        {
            экземпляр = new КурсорОверлей();
            диспетчерКурсора = Dispatcher.CurrentDispatcher;

            экземпляр.Show();

            готово.Set();

            // Собственный цикл сообщений Windows — независимый от
            // MainWindow. Именно это и убирает задержку.
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "СерьёзныйКурсорОверлей"
        };

        потокКурсора.SetApartmentState(ApartmentState.STA);
        потокКурсора.Start();

        готово.Wait(TimeSpan.FromSeconds(2));
    }

    public static void Остановить()
    {
        if (диспетчерКурсора == null)
            return;

        try
        {
            диспетчерКурсора.Invoke(() => экземпляр?.Close());
            диспетчерКурсора.InvokeShutdown();
        }
        catch
        {
            // Поток мог уже завершиться сам — не критично при выходе.
        }

        потокКурсора = null;
        диспетчерКурсора = null;
        экземпляр = null;
    }
}