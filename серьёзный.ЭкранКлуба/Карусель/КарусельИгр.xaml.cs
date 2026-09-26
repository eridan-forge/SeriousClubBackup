using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба.Карусель;

public partial class КарусельИгр : UserControl
{
    private const double ШиринаКарточкиБаза = 280;
    private const double ВысотаКарточкиБаза = 428;

    private const double УголНаШаг = 36;
    private double РадиусДугиБаза = 720;
    private double ГлубинаВыступаБаза = 130;

    private double базовыйМасштаб = 1.0;

    private const double МинимальныйМасштаб = 0.6;
    private const double ШагУменьшенияМасштаба = 0.16;

    private int видимыйДиапазон = 2;

    private bool обрабатыватьКолесо = true;

    // Пока true — карточкам за пределами видимыйДиапазон разрешено
    // оставаться отрисованными (с запасом +1.2), чтобы они успевали
    // появиться в кадре ДО того, как реально до него доедут. Как
    // только карусель останавливается — становится false, и всё,
    // что дальше видимыйДиапазон, скрывается полностью (Collapsed),
    // а не просто становится полупрозрачным где-то на периферии.
    private bool идётАнимацияПрокрутки;

    private readonly List<КарточкаИгрыКарусель> карточки = new();

    private int целевойИндекс;

    private int поколениеАнимации;

    private static readonly TimeSpan ЗадержкаКолеса = TimeSpan.FromMilliseconds(420);

    private DateTime последнийШагКолеса = DateTime.MinValue;

    public event Action<Игра>? ИграЗапущена;
    public event Action<Игра>? ИзбранноеИзменилось;
    public event Action<Игра>? ИграСкрыта;

    public event Action<Игра>? КарточкаОткрыта;

    public КарусельИгр()
    {
        InitializeComponent();
    }

    public void УстановитьМасштаб(double коэффициент)
    {
        if (коэффициент <= 0)
            коэффициент = 1;

        базовыйМасштаб = коэффициент;
    }

    public void УстановитьВидимыйДиапазон(int диапазон)
    {
        if (диапазон < 1)
            диапазон = 1;

        видимыйДиапазон = диапазон;
    }

    public void ОтключитьПрокруткуКолесом()
    {
        обрабатыватьКолесо = false;
    }

    public static readonly DependencyProperty ТекущийИндексProperty =
        DependencyProperty.Register(
            nameof(ТекущийИндекс),
            typeof(double),
            typeof(КарусельИгр),
            new PropertyMetadata(0.0, ОнТекущийИндексИзменился));

    public double ТекущийИндекс
    {
        get => (double)GetValue(ТекущийИндексProperty);
        set => SetValue(ТекущийИндексProperty, value);
    }

    private static void ОнТекущийИндексИзменился(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is КарусельИгр карусель)
            карусель.ПерепозиционироватьВсе();
    }

    public void Загрузить(
        IReadOnlyList<Игра> игры,
        ISet<Guid> избранные)
    {
        Guid? центральнаяРаньше =
            карточки.Count > 0 && целевойИндекс >= 0 && целевойИндекс < карточки.Count
                ? карточки[целевойИндекс].Игра?.Id
                : null;

        Сцена.Children.Clear();
        карточки.Clear();

        ТекстПусто.Visibility = игры.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        КнопкаНазад.Visibility = игры.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        КнопкаВперёд.Visibility = игры.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

        for (int i = 0; i < игры.Count; i++)
        {
            var игра = игры[i];
            var индексКарточки = i;

            var карточка = new КарточкаИгрыКарусель();

            карточка.Загрузить(игра, избранные.Contains(игра.Id));

            карточка.ИграЗапущена += игра2 => ИграЗапущена?.Invoke(игра2);
            карточка.ИзбранноеИзменилось += игра2 => ИзбранноеИзменилось?.Invoke(игра2);
            карточка.ИграСкрыта += игра2 => ИграСкрыта?.Invoke(игра2);
            карточка.КарточкаНажата += игра2 => КарточкаОткрыта?.Invoke(игра2);

            карточка.PreviewMouseLeftButtonDown += (_, ev) =>
            {
                if (индексКарточки != целевойИндекс)
                {
                    ПодкрутитьК(индексКарточки);
                    ev.Handled = true;
                }
            };

            карточки.Add(карточка);
            Сцена.Children.Add(карточка);
        }

        int новыйЦентральныйИндекс = 0;

        if (центральнаяРаньше.HasValue)
        {
            var найденный = карточки.FindIndex(x => x.Игра?.Id == центральнаяРаньше.Value);

            if (найденный >= 0)
                новыйЦентральныйИндекс = найденный;
        }

        целевойИндекс = новыйЦентральныйИндекс;

        идётАнимацияПрокрутки = false;

        поколениеАнимации++;
        BeginAnimation(ТекущийИндексProperty, null);

        if (Math.Abs(ТекущийИндекс - новыйЦентральныйИндекс) < 0.0001)
        {
            ПерепозиционироватьВсе();
        }
        else
        {
            ТекущийИндекс = новыйЦентральныйИндекс;
        }

        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(ПерепозиционироватьВсе));
    }

    private void КнопкаНазад_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.Now - последнийШагКолеса < ЗадержкаКолеса)
            return;

        последнийШагКолеса = DateTime.Now;
        ПодкрутитьК(целевойИндекс - 1);
    }

    private void КнопкаВперёд_Click(object sender, RoutedEventArgs e)
    {
        if (DateTime.Now - последнийШагКолеса < ЗадержкаКолеса)
            return;

        последнийШагКолеса = DateTime.Now;
        ПодкрутитьК(целевойИндекс + 1);
    }

    private void Карусель_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!обрабатыватьКолесо)
            return;

        e.Handled = true;

        if (DateTime.Now - последнийШагКолеса < ЗадержкаКолеса)
            return;

        последнийШагКолеса = DateTime.Now;

        ПодкрутитьК(целевойИндекс + (e.Delta < 0 ? 1 : -1));
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Left)
        {
            ПодкрутитьК(целевойИндекс - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            ПодкрутитьК(целевойИндекс + 1);
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    private void ПодкрутитьК(int индекс)
    {
        if (карточки.Count == 0)
            return;

        int количество = карточки.Count;

        double лучший = индекс;
        double лучшаяРазница = Math.Abs(индекс - ТекущийИндекс);

        foreach (var вариант in new[] { индекс - количество, индекс + количество })
        {
            var разница = Math.Abs(вариант - ТекущийИндекс);

            if (разница < лучшаяРазница)
            {
                лучшаяРазница = разница;
                лучший = вариант;
            }
        }

        целевойИндекс = ((индекс % количество) + количество) % количество;

        идётАнимацияПрокрутки = true;

        var моёПоколение = ++поколениеАнимации;

        var анимация = new DoubleAnimation(
            ТекущийИндекс,
            лучший,
            TimeSpan.FromMilliseconds(460))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        анимация.Completed += (_, _) =>
        {
            if (моёПоколение != поколениеАнимации)
                return;

            var нормализованное = ((ТекущийИндекс % количество) + количество) % количество;

            // Важно выставить false ДО финального BeginAnimation(null) —
            // именно этот флаг определяет, применится ли уже сейчас,
            // на последнем кадре, строгий (без запаса) порог видимости.
            идётАнимацияПрокрутки = false;

            BeginAnimation(ТекущийИндексProperty, null);
            ТекущийИндекс = нормализованное;

            // Подстраховка: если новое значение оказалось равно уже
            // установленному (PropertyChangedCallback тогда не
            // сработает), пересчитываем раскладку явно — иначе дальняя
            // карточка может остаться видимой ещё один кадр после
            // остановки.
            ПерепозиционироватьВсе();
        };

        BeginAnimation(ТекущийИндексProperty, анимация);
    }

    private void Сцена_SizeChanged(object sender, SizeChangedEventArgs e) => ПерепозиционироватьВсе();

    private void ПерепозиционироватьВсе()
    {
        if (карточки.Count == 0)
            return;

        double центрX = Сцена.ActualWidth / 2 - 18 * базовыйМасштаб;
        double центрY = Сцена.ActualHeight / 2;

        int количество = карточки.Count;

        double радиусДуги = РадиусДугиБаза * базовыйМасштаб;
        double глубинаВыступа = ГлубинаВыступаБаза * базовыйМасштаб;
        double ширинаКарточки = ШиринаКарточкиБаза * базовыйМасштаб;
        double высотаКарточки = ВысотаКарточкиБаза * базовыйМасштаб;

        // Пока карусель едет — держим запас в 1.2 позиции, чтобы
        // следующая карточка успела появиться до того, как реально
        // доедет до края видимой зоны. В покое запаса нет вообще:
        // всё, что дальше видимыйДиапазон, скрывается полностью.
        double порогВидимости = идётАнимацияПрокрутки
            ? видимыйДиапазон + 1.2
            : видимыйДиапазон;

        for (int i = 0; i < количество; i++)
        {
            double смещение = СмещениеСУчётомКольца(i, ТекущийИндекс, количество);

            var карточка = карточки[i];

            if (Math.Abs(смещение) > порогВидимости)
            {
                карточка.Visibility = Visibility.Collapsed;
                continue;
            }

            карточка.Visibility = Visibility.Visible;

            double уголРад = смещение * (УголНаШаг * Math.PI / 180.0);

            double x = Math.Sin(уголРад) * радиусДуги;

            double выступВперёд = (1 - Math.Cos(Math.Abs(уголРад))) * глубинаВыступа;

            double абсСмещение = Math.Abs(смещение);

            double масштабФормы = Math.Max(МинимальныйМасштаб, 1.0 - абсСмещение * ШагУменьшенияМасштаба);

            double поворотY = Math.Clamp(смещение * УголНаШаг * 1.3, -62, 62);

            Canvas.SetLeft(карточка, центрX + x - (ширинаКарточки * масштабФормы / 2));
            Canvas.SetTop(карточка, центрY + выступВперёд * 0.35 - (высотаКарточки * масштабФормы / 2));

            карточка.МасштабКарточки = масштабФормы * базовыйМасштаб;
            карточка.УголПоворота = поворотY;

            карточка.РадиусСкругления = 8 + Math.Min(абсСмещение, 3) * 12;

            карточка.Opacity = Math.Max(0.35, 1.0 - абсСмещение * 0.22);

            Panel.SetZIndex(карточка, (int)Math.Round((видимыйДиапазон + 3 - абсСмещение) * 100));
        }
    }

    private static double СмещениеСУчётомКольца(int индекс, double центр, int количество)
    {
        double смещение = индекс - центр;

        if (количество <= 1)
            return смещение;

        foreach (var вариант in new[] { смещение - количество, смещение + количество })
        {
            if (Math.Abs(вариант) < Math.Abs(смещение))
                смещение = вариант;
        }

        return смещение;
    }
}