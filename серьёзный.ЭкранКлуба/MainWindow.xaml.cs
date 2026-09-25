using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using серьёзный.Модели;
using серьёзный.Сервисы;
using серьёзный.ЭкранКлуба.Модели;
using серьёзный.ЭкранКлуба.Сервисы;
using System.Text.Json;
using серьёзный.Core.CoreServices;
using System.Threading.Tasks;
using серьёзный.Core.CoreComputers;
using серьёзный.Core.CoreThemes;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreWeather;

namespace серьёзный.ЭкранКлуба
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer таймерЧасов = new();
        private readonly DispatcherTimer наблюдение = new();
        private readonly DispatcherTimer патрульНаблюдение = new();
        private readonly DispatcherTimer темаНаблюдение = new();
        private readonly DispatcherTimer погодаНаблюдение = new();

        
        private Config config = new();
        private State state = new();

        private bool explorerЗапущен;
        private bool прошлоеСостояние = true;
        private bool окноИгрокаАктивно;
        private bool идётФорматированиеТелефона;
        private bool парольВиден;
        private bool погодаОбновляется;
        private WeatherDto? последняяПогода;

        private string? текущаяТемаId;

        // ---- ПЛЕЙЛИСТ ВИДЕО ТЕКУЩЕЙ ТЕМЫ (двойная буферизация) ----
        private readonly List<string> видеоТемы = new();
        private int индексВидео;
        private int индексСледующего;
        private bool аЭлементАктивен = true;
        private int неудачныхВидеоПодряд;

        private static readonly TimeZoneInfo МосковскийПояс = ПолучитьМосковскийПояс();

        private static readonly CultureInfo РусскаяКультура = new("ru-RU");

        private bool ЭтоПервыйЗапускShell =>
            (Application.Current as App)?.ЭтоПервыйЗапускShell == true;

        public MainWindow()
        {
            PatrolProcessLauncher.ЗапуститьЕслиНужно();

            InitializeComponent();

            Loaded += ПриЗагрузке;
            Closing += (_, e) => e.Cancel = true;
        }

        private static TimeZoneInfo ПолучитьМосковскийПояс()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"); }
            catch
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"); }
                catch { return TimeZoneInfo.Local; }
            }
        }

        private void ПриЗагрузке(object sender, RoutedEventArgs e)
        {
            ОбновитьНастройки();

            ПрименитьТемуПоId(config.ThemeId);

            ЗапуститьМерцаниеРамок();

            try
            {
                ЗапуститьПрослушиваниеПередачи();
            }
            catch (Exception ошибка)
            {
                ЗаписатьЛогИнициализации(
                     "Синхронизация игр (UDP) не запущена: " + ошибка);
            }

            ОбновитьЧасы();

            таймерЧасов.Interval = TimeSpan.FromSeconds(1);
            таймерЧасов.Tick += (_, _) => ОбновитьЧасы();
            таймерЧасов.Start();

            наблюдение.Interval = TimeSpan.FromMilliseconds(250);
            наблюдение.Tick += (_, _) =>
            {
                try { ПроверитьСостояние(); }
                catch { }
            };
            наблюдение.Start();

            патрульНаблюдение.Interval = TimeSpan.FromSeconds(8);
            патрульНаблюдение.Tick += (_, _) =>
            {
                try { PatrolProcessLauncher.ЗапуститьЕслиНужно(); }
                catch { }
            };
            патрульНаблюдение.Start();

            темаНаблюдение.Interval = TimeSpan.FromSeconds(3);
            темаНаблюдение.Tick += (_, _) =>
            {
                try
                {
                    var свежийКонфиг = ConfigService.Загрузить();

                    if (свежийКонфиг.ThemeId != текущаяТемаId)
                        ПрименитьТемуПоId(свежийКонфиг.ThemeId);
                }
                catch { }
            };
            темаНаблюдение.Start();

            // Первый запрос сразу. Дальше интервал сам себе выставит
            // ОбновитьПогодуAsync: 30 минут, если ответил сервер, или
            // 10 минут проверки сервера, пока его нет (см. ниже).
            _ = ОбновитьПогодуAsync();

            погодаНаблюдение.Interval = ИнтервалПроверкиСервера;
            погодаНаблюдение.Tick += async (_, _) => await ОбновитьПогодуAsync();
            погодаНаблюдение.Start();
        }

        // =====================================================
        // ЧАСЫ / ДАТА / ДЕНЬ НЕДЕЛИ
        // =====================================================

        private void ОбновитьЧасы()
        {
            var сейчас = TimeZoneInfo.ConvertTime(DateTime.Now, МосковскийПояс);

            // "HH:mm" = 24 часа. Нужен 12-часовой — поставь "hh:mm".
            // Нужны секунды — "HH:mm:ss".
            Часы.Text = сейчас.ToString("HH:mm");

            ТекстДата.Text = сейчас.ToString("d MMMM yyyy", РусскаяКультура);

            ТекстДеньНедели.Text =
                сейчас.ToString("dddd", РусскаяКультура).ToUpper(РусскаяКультура);
        }

        

        // =====================================================
        // ТЕМЫ ЭКРАНА ВХОДА (с плейлистом видео, двойная буферизация)
        // =====================================================

        private MediaElement Активный => аЭлементАктивен ? ФонВидеоА : ФонВидеоБ;

        private MediaElement Ожидающий => аЭлементАктивен ? ФонВидеоБ : ФонВидеоА;

        private void ПрименитьТемуПоId(string? themeId)
        {
            текущаяТемаId = themeId;

            видеоТемы.Clear();

            СброситьЭлемент(ФонВидеоА);
            СброситьЭлемент(ФонВидеоБ);

            ФонКартинка.Source = null;
            ФонКартинка.Visibility = Visibility.Collapsed;

            ЛогоМаска.ImageSource = null;
            ЛогоФигура.Visibility = Visibility.Collapsed;

            var тема = string.IsNullOrWhiteSpace(themeId)
                ? null
                : ЗагрузчикТем.НайтиПоId(themeId);

            if (тема == null && !string.IsNullOrWhiteSpace(themeId))
            {
                ЗаписатьЛогТемы(
                    $"Тема «{themeId}» не найдена ни в Темы\\ рядом с exe, " +
                    "ни в %ProgramData%\\SeriousClub\\Themes\\. Подставляю тему по умолчанию.");
            }

            тема ??= ЗагрузчикТем.НайтиПоУмолчанию();

            if (тема == null)
            {
                ЗаписатьЛогТемы(
                    "Тем не найдено ни в Темы\\ рядом с exe, ни в " +
                    "%ProgramData%\\SeriousClub\\Themes\\ — экран без фона/лого.");

                return;
            }

            if (тема.ФонЭтоВидео)
            {
                видеоТемы.AddRange(тема.Видео);

                ЗаписатьЛогТемы(
                    $"Тема «{тема.Id}»: видео в плейлисте — {видеоТемы.Count}.");

                ЗапуститьПлейлист();
            }
            else
            {
                if (File.Exists(тема.ПутьФонИзображение))
                {
                    try
                    {
                        var картинка = new BitmapImage();

                        картинка.BeginInit();
                        картинка.CacheOption = BitmapCacheOption.OnLoad;
                        картинка.UriSource = new Uri(тема.ПутьФонИзображение);
                        картинка.EndInit();
                        картинка.Freeze();

                        ФонКартинка.Source = картинка;
                        ФонКартинка.Visibility = Visibility.Visible;
                    }
                    catch (Exception ошибка)
                    {
                        ЗаписатьЛогТемы($"Не удалось загрузить фон темы «{тема.Id}»: {ошибка}");
                    }
                }
                else
                {
                    ЗаписатьЛогТемы($"Файл фона темы «{тема.Id}» не найден: {тема.ПутьФонИзображение}");
                }
            }

            if (File.Exists(тема.ПутьЛого))
            {
                try
                {
                    ЛогоМаска.ImageSource = СделатьЧёрныйФонПрозрачным(тема.ПутьЛого);
                    ЛогоФигура.Visibility = Visibility.Visible;
                }
                catch (Exception ошибка)
                {
                    ЗаписатьЛогТемы($"Не удалось обработать logo.png темы «{тема.Id}»: {ошибка}");
                }
            }
            else
            {
                ЗаписатьЛогТемы($"logo.png темы «{тема.Id}» не найден: {тема.ПутьЛого}");
            }
        }

        // Запускает плейлист с чистого листа: первый ролик играет
        // сразу в "активном" элементе, второй элемент в это же время
        // открывает и замирает на первом кадре следующего ролика — даже
        // если в теме всего ОДНО видео (тогда оба элемента открывают
        // тот же файл, но НЕЗАВИСИМО друг от друга, поэтому переключение
        // всё равно мгновенное, без повторной буферизации).
        private void ЗапуститьПлейлист()
        {
            неудачныхВидеоПодряд = 0;

            СброситьЭлемент(ФонВидеоА);
            СброситьЭлемент(ФонВидеоБ);

            if (видеоТемы.Count == 0)
                return;

            аЭлементАктивен = true;
            индексВидео = 0;
            индексСледующего = СледующийИндекс(индексВидео);

            ОткрытьВЭлемент(Активный, видеоТемы[индексВидео], играть: true);
            ОткрытьВЭлемент(Ожидающий, видеоТемы[индексСледующего], играть: false);
        }

        private int СледующийИндекс(int индекс)
        {
            if (видеоТемы.Count == 0)
                return 0;

            return (индекс + 1) % видеоТемы.Count;
        }

        private static void СброситьЭлемент(MediaElement элемент)
        {
            try { элемент.Stop(); } catch { }

            элемент.Source = null;
            элемент.Visibility = Visibility.Collapsed;
        }

        // Открывает файл в элементе. играть=true — этот должен пойти на
        // экран сразу после открытия; играть=false — предзагрузка,
        // замирает на первом кадре. Source ОБЯЗАТЕЛЬНО сбрасывается в
        // null перед новым назначением — без этого повторная установка
        // ТОГО ЖЕ пути (тема с одним видео, либо повтор плейлиста по
        // кругу) не считалась бы WPF изменением свойства, MediaOpened
        // не сработал бы повторно, и ролик застыл бы на последнем кадре
        // навсегда — это и был старый баг.
        private void ОткрытьВЭлемент(MediaElement элемент, string путь, bool играть)
        {
            if (!File.Exists(путь))
            {
                if (играть)
                    ПропуститьСломанноеАктивное("файл не найден: " + путь);
                else
                    ПропуститьСломанноеОжидающее("файл не найден: " + путь);

                return;
            }

            элемент.Stop();
            элемент.Source = null;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    элемент.Source = new Uri(путь);
                    элемент.Position = TimeSpan.Zero;
                }
                catch (Exception ошибка)
                {
                    var причина = $"не удалось открыть {путь}: {ошибка.Message}";

                    if (играть)
                        ПропуститьСломанноеАктивное(причина);
                    else
                        ПропуститьСломанноеОжидающее(причина);
                }
            }));
        }

        private void ФонВидео_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement элемент)
                return;

            неудачныхВидеоПодряд = 0;

            if (ReferenceEquals(элемент, Активный))
            {
                элемент.Visibility = Visibility.Visible;
                элемент.Play();
            }
            else
            {
                // Предзагрузка: доводим до первого кадра и замираем —
                // когда придёт момент переключения, Play() на уже
                // открытом файле сработает мгновенно, без буферизации.
                элемент.Play();
                элемент.Position = TimeSpan.Zero;
                элемент.Pause();
            }
        }

        // Ролик доиграл до конца — переключаемся на уже готовый (открытый
        // и стоящий на паузе) второй элемент. Само переключение —
        // синхронно и мгновенно, без ожидания открытия файла.
        private void ФонВидео_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (sender is not MediaElement завершившийся)
                return;

            if (!ReferenceEquals(завершившийся, Активный))
                return; // событие от уже неактивного элемента — игнорируем

            неудачныхВидеоПодряд = 0;

            ПереключитьНаСледующее();
        }

        private void ПереключитьНаСледующее()
        {
            if (видеоТемы.Count == 0)
                return;

            var новыйАктивный = Ожидающий;
            var освобождающийся = Активный;

            аЭлементАктивен = !аЭлементАктивен;

            индексВидео = индексСледующего;
            индексСледующего = СледующийИндекс(индексВидео);

            освобождающийся.Stop();
            освобождающийся.Visibility = Visibility.Collapsed;

            if (новыйАктивный.Source != null)
            {
                // Уже открыт и стоит на первом кадре — мгновенное переключение.
                новыйАктивный.Position = TimeSpan.Zero;
                новыйАктивный.Visibility = Visibility.Visible;
                новыйАктивный.Play();
            }
            else
            {
                // Предзагрузка почему-то не успела (очень короткий ролик) —
                // редкий случай, откроем напрямую, будет короткая пауза.
                ОткрытьВЭлемент(новыйАктивный, видеоТемы[индексВидео], играть: true);
            }

            // Предзагружаем следующий по очереди ролик на освободившемся элементе.
            ОткрытьВЭлемент(освобождающийся, видеоТемы[индексСледующего], играть: false);
        }

        private void ФонВидео_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is not MediaElement элемент)
                return;

            ЗаписатьЛогМедиа(
                ReferenceEquals(элемент, ФонВидеоА) ? "ФонВидеоА" : "ФонВидеоБ",
                e.ErrorException);

            var причина = "ошибка воспроизведения: " + (e.ErrorException?.Message ?? "неизвестно");

            if (ReferenceEquals(элемент, Активный))
                ПропуститьСломанноеАктивное(причина);
            else
                ПропуститьСломанноеОжидающее(причина);
        }

        // Битый/недоступный ролик в АКТИВНОМ (играющем) слоте —
        // сразу переходим к следующему по очереди видео.
        private void ПропуститьСломанноеАктивное(string причина)
        {
            ЗаписатьЛогТемы("Активное видео пропущено — " + причина);

            неудачныхВидеоПодряд++;

            if (видеоТемы.Count == 0 || неудачныхВидеоПодряд >= видеоТемы.Count)
            {
                СкрытьВидеоЦеликом();
                return;
            }

            индексВидео = СледующийИндекс(индексВидео);
            индексСледующего = СледующийИндекс(индексВидео);

            СброситьЭлемент(Активный);
            СброситьЭлемент(Ожидающий);

            ОткрытьВЭлемент(Активный, видеоТемы[индексВидео], играть: true);
            ОткрытьВЭлемент(Ожидающий, видеоТемы[индексСледующего], играть: false);
        }

        // Битый ролик в СЛОТЕ ПРЕДЗАГРУЗКИ — то, что уже играет, не
        // трогаем, просто пробуем предзагрузить следующий по очереди.
        private void ПропуститьСломанноеОжидающее(string причина)
        {
            ЗаписатьЛогТемы("Предзагрузка ролика пропущена — " + причина);

            if (видеоТемы.Count == 0)
                return;

            индексСледующего = СледующийИндекс(индексСледующего);

            if (индексСледующего == индексВидео)
                return; // прошли по кругу, работающего не нашли — активное не трогаем

            ОткрытьВЭлемент(Ожидающий, видеоТемы[индексСледующего], играть: false);
        }

        private void СкрытьВидеоЦеликом()
        {
            СброситьЭлемент(ФонВидеоА);
            СброситьЭлемент(ФонВидеоБ);
        }

        private static BitmapSource СделатьЧёрныйФонПрозрачным(
            string путь,
            byte порог = 24)
        {
            var исходное = new BitmapImage();

            исходное.BeginInit();
            исходное.CacheOption = BitmapCacheOption.OnLoad;
            исходное.UriSource = new Uri(путь);
            исходное.EndInit();
            исходное.Freeze();

            var конверт = new FormatConvertedBitmap(
                исходное,
                PixelFormats.Bgra32,
                null,
                0);

            int width = конверт.PixelWidth;
            int height = конверт.PixelHeight;
            int stride = width * 4;

            var pixels = new byte[height * stride];

            конверт.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                if (r <= порог && g <= порог && b <= порог)
                {
                    pixels[i] = 0;
                    pixels[i + 1] = 0;
                    pixels[i + 2] = 0;
                    pixels[i + 3] = 0;
                }
            }

            var результат = BitmapSource.Create(
                width,
                height,
                конверт.DpiX,
                конверт.DpiY,
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);

            результат.Freeze();

            return результат;
        }

        private static void ЗаписатьЛогТемы(string сообщение)
        {
            try
            {
                var папка = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub", "logs");

                Directory.CreateDirectory(папка);

                File.AppendAllText(
                    Path.Combine(папка, "club-screen-crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Тема входа: {сообщение}{Environment.NewLine}");
            }
            catch { }
        }

        private static void ЗаписатьЛогМедиа(string слой, Exception? ошибка)
        {
            try
            {
                var папка = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub", "logs");

                Directory.CreateDirectory(папка);

                File.AppendAllText(
                    Path.Combine(папка, "club-screen-crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MediaFailed ({слой}): {ошибка}{Environment.NewLine}");
            }
            catch { }
        }

        private static void ЗаписатьЛогИнициализации(string сообщение)
        {
            try
            {
                var папка = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub", "logs");

                Directory.CreateDirectory(папка);

                File.AppendAllText(
                    Path.Combine(папка, "club-screen-crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Инициализация: {сообщение}{Environment.NewLine}");
            }
            catch { }
        }

        // =====================================================
        // МЕРЦАНИЕ КРАСНЫХ РАМОК У ПОЛЕЙ ВВОДА
        // =====================================================
        private void ЗапуститьМерцаниеРамок()
        {
            var кисть =
                FindName("ОгненнаяРамкаКисть") as SolidColorBrush
                ?? Resources["ОгненнаяРамкаКисть"] as SolidColorBrush;

            if (кисть == null)
                return;

            var анимация = new ColorAnimation
            {
                From = Color.FromRgb(0x5C, 0x18, 0x26), // спокойное состояние
                To = Color.FromRgb(0xFF, 0x3D, 0x66),   // вспышка
                Duration = TimeSpan.FromSeconds(1.8),   // скорость перелива
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            кисть.BeginAnimation(SolidColorBrush.ColorProperty, анимация);
        }

        // =====================================================
        // НАСТРОЙКИ / СОСТОЯНИЕ ЭКРАНА
        // =====================================================

        private void ОбновитьНастройки()
        {
            config = ConfigService.Загрузить();
            state = StateService.Загрузить();

            var карточкаПК = КартаКомпьютеров.НайтиПоId(state.PcId);

            ИмяПК.Text = карточкаПК?.Название ?? $"ПК-{state.PcId}";

            if (ЭтоПервыйЗапускShell)
            {
                state.Locked = true;
                StateService.Сохранить(state);
            }

            прошлоеСостояние = state.Locked;

            if (state.Locked)
                Заблокировать();
            else
                ОбработатьРазблокировку(state);
        }

        private void ПроверитьСостояние()
        {
            state = StateService.Загрузить();

            if (state.Locked == прошлоеСостояние)
                return;

            прошлоеСостояние = state.Locked;

            if (state.Locked)
                Заблокировать();
            else
                ОбработатьРазблокировку(state);
        }

        private void Разблокировать()
        {
            if (!explorerЗапущен)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    UseShellExecute = true
                });

                explorerЗапущен = true;
            }

            Hide();
        }

        private void ОбработатьРазблокировку(State текущее)
        {
            if (окноИгрокаАктивно)
                return;

            if (текущее.AccountId.HasValue && текущее.AccountId.Value != Guid.Empty)
                ОткрытьОкноИгрока(текущее.AccountId.Value, текущее.PcId);
            else
                Разблокировать();
        }

        private void ОткрытьОкноИгрока(Guid accountId, int pcId)
        {
            if (окноИгрокаАктивно)
                return;

            окноИгрокаАктивно = true;

            state.Locked = false;
            state.AccountId = accountId;
            StateService.Сохранить(state);

            прошлоеСостояние = false;

            var окноИгрока = new ОкноИгрока(accountId, pcId);

            // Раньше Hide() стоял ДО создания ОкноИгрока — окно входа исчезало
            // мгновенно, а новое окно ещё секунду строило XAML и делало первый
            // запрос аккаунта. В этот зазор был виден настоящий рабочий стол
            // Windows (explorer.exe уже когда-то был запущен Разблокировать()).
            // Теперь прячем окно входа только когда окно игрока реально
            // отрисовало первый кадр — оно полноэкранное, значит уже полностью
            // закрывает собой экран входа, и Hide() ничего не открывает под собой.
            окноИгрока.ContentRendered += (_, _) =>
            {
                if (IsVisible)
                    Hide();
            };

            try
            {
                окноИгрока.ShowDialog();
            }
            finally
            {
                окноИгрокаАктивно = false;
            }

            state = StateService.Загрузить();
            state.Locked = true;
            state.AccountId = null;
            StateService.Сохранить(state);

            прошлоеСостояние = true;

            Заблокировать();
        }

        private void Заблокировать()
        {
            Show();
            WindowState = WindowState.Maximized;
            Topmost = true;
            Activate();
            explorerЗапущен = true;

            СброситьСостояниеВхода();
        }

        // =====================================================
        // КЛИК В ЛЮБОЕ МЕСТО ПОЛЯ = ФОКУС НА ВВОД
        // =====================================================

        private void ПанельТелефона_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ПолеТелефон.Focus();
            ПолеТелефон.CaretIndex = ПолеТелефон.Text.Length;
        }

        private void ПанельПароля_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Клик по «глазу» сюда не долетает: Button гасит MouseLeftButtonDown.
            if (парольВиден)
            {
                ПолеПарольВидимый.Focus();
                ПолеПарольВидимый.CaretIndex = ПолеПарольВидимый.Text.Length;
            }
            else
            {
                ПолеПароль.Focus();
            }
        }

        // Enter в любом поле = нажать «ВОЙТИ».
        private void ПолеВвода_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            if (КнопкаВойти.IsEnabled)
                Войти_Click(КнопкаВойти, new RoutedEventArgs());
        }

        // =====================================================
        // ФОРМАТИРОВАНИЕ ТЕЛЕФОНА
        // =====================================================

        private void ПолеТелефон_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (идётФорматированиеТелефона)
                return;

            идётФорматированиеТелефона = true;

            try
            {
                var курсорВКонце = ПолеТелефон.CaretIndex == ПолеТелефон.Text.Length;
                var цифры = ИзвлечьСырыеЦифры(ПолеТелефон.Text);
                var отформатировано = ФорматТелефона(цифры);

                ПолеТелефон.Text = отформатировано;

                ПолеТелефон.CaretIndex = курсорВКонце
                    ? отформатировано.Length
                    : Math.Min(ПолеТелефон.CaretIndex, отформатировано.Length);

                if (ПодсказкаТелефон != null)
                {
                    ПодсказкаТелефон.Visibility = string.IsNullOrEmpty(ПолеТелефон.Text)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
            finally
            {
                идётФорматированиеТелефона = false;
            }
        }

        private static string ИзвлечьСырыеЦифры(string текст)
        {
            var цифры = new string((текст ?? string.Empty).Where(char.IsDigit).ToArray());

            if (цифры.Length > 11)
                цифры = цифры.Substring(0, 11);

            return цифры;
        }

        private static string НормализоватьДляВхода(string цифры)
        {
            if (цифры.Length == 0)
                return string.Empty;

            if (цифры[0] == '8')
                цифры = "7" + цифры.Substring(1);
            else if (цифры[0] != '7')
                цифры = "7" + цифры;

            if (цифры.Length > 11)
                цифры = цифры.Substring(0, 11);

            return цифры;
        }

        private static string ФорматТелефона(string цифры)
        {
            if (цифры.Length == 0)
                return string.Empty;

            var sb = new StringBuilder("+");
            sb.Append(цифры[0]);

            if (цифры.Length > 1)
                sb.Append(" (").Append(цифры.Substring(1, Math.Min(3, цифры.Length - 1)));

            if (цифры.Length > 4)
                sb.Append(") ").Append(цифры.Substring(4, Math.Min(3, цифры.Length - 4)));

            if (цифры.Length > 7)
                sb.Append("-").Append(цифры.Substring(7, Math.Min(2, цифры.Length - 7)));

            if (цифры.Length > 9)
                sb.Append("-").Append(цифры.Substring(9, Math.Min(2, цифры.Length - 9)));

            return sb.ToString();
        }

        // =====================================================
        // ГЛАЗ — ПОКАЗАТЬ/СКРЫТЬ ПАРОЛЬ
        // =====================================================
        private void ПереключитьВидимостьПароля_Click(object sender, RoutedEventArgs e)
        {
            парольВиден = !парольВиден;

            if (парольВиден)
            {
                ПолеПарольВидимый.Text = ПолеПароль.Password;

                ПолеПароль.Visibility = Visibility.Collapsed;
                ПолеПарольВидимый.Visibility = Visibility.Visible;

                КнопкаПоказатьПароль.Content = "🙈";
            }
            else
            {
                ПолеПароль.Password = ПолеПарольВидимый.Text;

                ПолеПарольВидимый.Visibility = Visibility.Collapsed;
                ПолеПароль.Visibility = Visibility.Visible;

                КнопкаПоказатьПароль.Content = "👁";
            }
        }

        private string ПолучитьТекущийПароль()
        {
            return (парольВиден ? ПолеПарольВидимый.Text : ПолеПароль.Password).Trim();
        }

        // =====================================================
        // ВХОД
        // =====================================================

        private async void Войти_Click(object sender, RoutedEventArgs e)
        {
            ТекстОшибка.Visibility = Visibility.Collapsed;

            var телефон = ПолеТелефон.Text;
            var пароль = ПолучитьТекущийПароль();

            // ---------------------------------------------------------------
                        // ТЕСТОВЫЙ АККАУНТ — единственный захардкоженный, открывает
                        // ОкноИгрока напрямую, в обход сервера и Патруля. Магазин/чат/
                        // экономика внутри окна попробуют достучаться до сервера как
                        // обычно и молча не получат ответа — это ожидаемо, у тестового
                        // аккаунта нет реальной строки в таблице Accounts.
                        // ---------------------------------------------------------------
                        if (НормализоватьДляВхода(ИзвлечьСырыеЦифры(телефон)) == ТестовыйАккаунт.Телефон &&
            пароль == ТестовыйАккаунт.Пароль)
                            {
                ОчиститьПароль();
                ОткрытьОкноИгрока(ТестовыйАккаунт.Id, state.PcId);
                                return;
                            }

            if (ИзвлечьСырыеЦифры(телефон).Length < 10)
            {
                ПоказатьОшибку("Введите корректный номер телефона.");
                return;
            }

            if (string.IsNullOrWhiteSpace(пароль))
            {
                ПоказатьОшибку("Введите пароль.");
                return;
            }

            КнопкаВойти.IsEnabled = false;
            ПоказатьОшибку("Проверка...");
            ТекстОшибка.Foreground = Brushes.LightGray;

            LoginRequestRecord? результат = null;
            string? ошибкаЗапроса = null;

            try
            {
                var requestId = AccountLoginBridgeService.CreateRequest(
                    НормализоватьДляВхода(ИзвлечьСырыеЦифры(телефон)),
                    пароль);

                for (int i = 0; i < 150; i++)
                {
                    await Task.Delay(100);

                    результат = AccountLoginBridgeService.GetResult(requestId);

                    if (результат != null && результат.Status != LoginRequestStatus.Pending)
                        break;
                }
            }
            catch (Exception ошибка)
            {
                ошибкаЗапроса = ошибка.Message;
                ЗаписатьЛогВхода(ошибка);
            }
            finally
            {
                КнопкаВойти.IsEnabled = true;
                ТекстОшибка.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x5C, 0x5C));
            }

            if (ошибкаЗапроса != null)
            {
                ПоказатьОшибку("Ошибка входа: " + ошибкаЗапроса);
                ОчиститьПароль();
                return;
            }

            if (результат == null || результат.Status == LoginRequestStatus.Pending)
            {
                ПоказатьОшибку("Сервер не ответил. Попробуйте ещё раз.");
                ОчиститьПароль();
                return;
            }

            if (результат.Status == LoginRequestStatus.Failed || !результат.AccountId.HasValue)
            {
                ПоказатьОшибку(результат.Error ?? "Неверный номер телефона или пароль.");
                ОчиститьПароль();
                return;
            }

            ТекстОшибка.Visibility = Visibility.Collapsed;
            ОчиститьПароль();

            ОткрытьОкноИгрока(результат.AccountId.Value, state.PcId);
        }

        private void ОчиститьПароль()
        {
            ПолеПароль.Password = "";
            ПолеПарольВидимый.Text = "";
        }

        private void СброситьСостояниеВхода()
        {
            if (КнопкаВойти == null)
                return;

            КнопкаВойти.IsEnabled = true;

            if (ТекстОшибка != null)
                ТекстОшибка.Visibility = Visibility.Collapsed;
        }

        private static void ЗаписатьЛогВхода(Exception ошибка)
        {
            try
            {
                var папка = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub", "logs");

                Directory.CreateDirectory(папка);

                File.AppendAllText(
                    Path.Combine(папка, "club-screen-crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Ошибка входа: {ошибка}{Environment.NewLine}");
            }
            catch { }
        }

        private void ПоказатьОшибку(string текст)
        {
            ТекстОшибка.Text = текст;
            ТекстОшибка.Visibility = Visibility.Visible;
        }

        // =====================================================
        // ССЫЛКИ ПОД ФОРМОЙ
        // =====================================================

        private void ЗабылиПароль_Click(object sender, RoutedEventArgs e)
        {
            new ИнфоОкно(
                "Обратитесь к администратору клуба, чтобы сбросить пароль.",
                "Забыли пароль?")
            {
                Owner = this
            }.ShowDialog();
        }

        private void Зарегистрироваться_Click(object sender, RoutedEventArgs e)
        {
            new ИнфоОкно(
                "Для создания аккаунта обратитесь к администратору клуба.",
                "Регистрация")
            {
                Owner = this
            }.ShowDialog();
        }

        private void ВойтиКакГость_Click(object sender, RoutedEventArgs e)
        {
            new ИнфоОкно(
                "Обратитесь к администратору клуба с просьбой начать вам сессию не создавая себе аккаунт.",
                "Гостевой вход")
            {
                Owner = this
            }.ShowDialog();
        }

        private void Обслуживание_Click(object sender, RoutedEventArgs e)
        {
            СброситьСостояниеВхода();

            config = ConfigService.Загрузить();

            var окно = new PasswordWindow(config.Password)
            {
                Owner = this,
                Topmost = !Debugger.IsAttached
            };

            окно.Loaded += (_, _) => окно.Activate();

            окно.ShowDialog();
        }

        private void ЗапуститьПрослушиваниеПередачи()
        {
            GameSyncService.StartListening((pcId, games) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (pcId != state.PcId)
                        return;

                    GameCacheService.Store(pcId, games);
                });
            });
        }

        // =====================================================
        // ПОГОДА
        //
        // Обычный режим: погоду считает СЕРВЕР (у него кэш 30 минут),
        // экран только спрашивает его по локальной сети и наружу не
        // ходит вообще.
        //
        // Запасной режим: сервер/Патруль недоступны — экран идёт в
        // интернет сам, но НЕ чаще раза в час. Сервер при этом
        // проверяется раз в 10 минут: как только он появится, экран
        // сам вернётся к общим данным и перестанет ходить наружу.
        // =====================================================

        private static readonly TimeSpan ИнтервалСПодключённымСервером =
            TimeSpan.FromMinutes(30);

        private static readonly TimeSpan ИнтервалПроверкиСервера =
            TimeSpan.FromMinutes(10);

        private static readonly TimeSpan МинимумМеждуСобственнымиЗапросами =
            TimeSpan.FromHours(1);

        private DateTime времяСобственногоЗапроса = DateTime.MinValue;

        private async Task ОбновитьПогодуAsync()
        {
            if (погодаОбновляется)
                return;

            погодаОбновляется = true;

            try
            {
                var отСервера = await СпроситьПогодуУСервераAsync();

                if (отСервера != null && отСервера.Успешно)
                {
                    погодаНаблюдение.Interval = ИнтервалСПодключённымСервером;

                    последняяПогода = отСервера;

                    ОтобразитьПогоду(отСервера);

                    return;
                }

                // Сервера нет — проверяем его почаще, чтобы вернуться
                // к общим данным сразу, как только он появится.
                погодаНаблюдение.Interval = ИнтервалПроверкиСервера;

                if (DateTime.Now - времяСобственногоЗапроса
                    < МинимумМеждуСобственнымиЗапросами)
                {
                    ПоказатьПогодуИлиПрочерк();
                    return;
                }

                var напрямую = await СервисПогодыСервера.ПолучитьAsync(
                    МинимумМеждуСобственнымиЗапросами);

                if (напрямую.Успешно)
                {
                    // Отметку ставим только на УСПЕХЕ: если интернета
                    // нет вообще, запрос до метеослужбы не доходит и
                    // никакой нагрузки на неё не создаёт — нет смысла
                    // блокировать повтор на целый час.
                    времяСобственногоЗапроса = DateTime.Now;

                    последняяПогода = напрямую;

                    ОтобразитьПогоду(напрямую);

                    return;
                }

                ПоказатьПогодуИлиПрочерк();
            }
            catch
            {
                // Погода — украшение, ронять экран входа она не имеет права.
            }
            finally
            {
                погодаОбновляется = false;
            }
        }

        private async Task<WeatherDto?> СпроситьПогодуУСервераAsync()
        {
            try
            {
                var requestId = WeatherBridgeService.CreateRequest();

                for (int i = 0; i < 20; i++) // до ~5 секунд
                {
                    await Task.Delay(250);

                    var результат = WeatherBridgeService.GetResult(requestId);

                    if (результат != null)
                        return результат;
                }
            }
            catch
            {
                // База занята/недоступна — считаем, что сервера нет.
            }

            return null;
        }

        private void ПоказатьПогодуИлиПрочерк()
        {
            if (последняяПогода != null)
            {
                // Держим последнее известное значение, чтобы панель
                // не мигала «нет данных» и обратно при каждом сбое.
                ОтобразитьПогоду(последняяПогода);
                return;
            }

            ПогодаИконка.Text = "❔";
            ПогодаТемпература.Text = "—°";
            ПогодаОписание.Text = "Нет данных";
            ПогодаМесто.Text = "с. Вилино";
        }

        private void ОтобразитьПогоду(WeatherDto погода)
        {
            var (описание, иконка) =
                ПогодаКоды.Расшифровать(погода.Код, погода.День);

            ПогодаИконка.Text = иконка;
            ПогодаОписание.Text = описание;

            ПогодаТемпература.Text =
                $"{(int)Math.Round(погода.Температура)}°";

            ПогодаМесто.Text =
                $"с. Вилино · {погода.Обновлено:HH:mm}";
        }
    }
}