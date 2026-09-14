using System;
using System.Diagnostics;
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

namespace серьёзный.ЭкранКлуба
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer таймерЧасов = new();
        private readonly DispatcherTimer наблюдение = new();
        private readonly DispatcherTimer патрульНаблюдение = new();
        private readonly DispatcherTimer темаНаблюдение = new(); // следит за сменой темы от админа

        private Config config = new();
        private State state = new();

        private bool explorerЗапущен;
        private bool прошлоеСостояние = true;
        private bool окноИгрокаАктивно;
        private bool идётФорматированиеТелефона;
        private bool парольВиден; // состояние кнопки-глаза

        private string? текущаяТемаId; // какая тема реально включена прямо сейчас

        private static readonly TimeZoneInfo МосковскийПояс = ПолучитьМосковскийПояс();

        private bool ЭтоПервыйЗапускShell =>
            (Application.Current as App)?.ЭтоПервыйЗапускShell == true;

        public MainWindow()
        {
            PatrolProcessLauncher.ЗапуститьЕслиНужно();

            InitializeComponent();

            Loaded += ПриЗагрузке;
            Closing += (_, e) => e.Cancel = true;
        }

        // =====================================================
        // МОСКОВСКОЕ ВРЕМЯ (тот же приём, что и в админке)
        // =====================================================
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

            ПрименитьТемуПоId(config.ThemeId); // включаем ту тему, что уже сохранена локально

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

            // "hh:mm" = 12-часовой формат без AM/PM (07:45, а не 19:45).
            // Хочешь секунды — поменяй на "hh:mm:ss".
            таймерЧасов.Interval = TimeSpan.FromSeconds(1);
            таймерЧасов.Tick += (_, _) =>
            {
                Часы.Text = TimeZoneInfo.ConvertTime(DateTime.Now, МосковскийПояс).ToString("hh:mm");
            };
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

            // Раз в 3 секунды проверяем — не прислал ли админ новую тему.
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
        }

        // =====================================================
        // ТЕМЫ ЭКРАНА ВХОДА
        // =====================================================

        // Никакого фолбэка на Assets\ больше нет — Темы\<Id>\ (или
        // %ProgramData%\SeriousClub\Themes\<Id>\) единственный источник.
        // Если темы нет/не назначена — экран просто остаётся без видео
        // и без лого (чёрный фон окна), а не показывает случайные
        // подставные файлы, которых может уже не быть на диске.
        private void ПрименитьТемуПоId(string? themeId)
        {
            текущаяТемаId = themeId;

            ФонВидео.Source = null;
            ЭффектВидео.Source = null;
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

            // Ничего явно не назначено (обычное состояние свежей базы, пока
            // админ ни разу не отправил тему) ИЛИ назначенная тема пропала
            // с диска — в обоих случаях подставляем тему по умолчанию,
            // чтобы экран никогда не оставался чёрным без видео. Работает
            // полностью локально: НайтиПоУмолчанию только сканирует папки
            // на диске, ни Патруль, ни сервер тут не участвуют.
            тема ??= ЗагрузчикТем.НайтиПоУмолчанию();

            if (тема == null)
            {
                ЗаписатьЛогТемы(
                    "Тем не найдено ни в Темы\\ рядом с exe, ни в " +
                    "%ProgramData%\\SeriousClub\\Themes\\ — экран без видео/лого.");

                return;
            }

            // ---- фон ----
            if (File.Exists(тема.ПутьФон))
            {
                ФонВидео.Source = new Uri(тема.ПутьФон);
                ФонВидео.Position = TimeSpan.Zero;
                ФонВидео.Play();
            }
            else
            {
                ЗаписатьЛогТемы($"bg.mp4 темы «{тема.Id}» не найден: {тема.ПутьФон}");
            }

            // ---- эффект поверх (пепел/угли) — Opacity вместо честного
            // screen-блендинга: MP4 не умеет альфа-канал, поэтому любой
            // непрозрачный фон видео перекроет дракон целиком независимо
            // от порядка слоёв. Полупрозрачность — рабочий компромисс
            // без кастомного шейдера. ----
            if (File.Exists(тема.ПутьЭффект))
            {
                ЭффектВидео.Source = new Uri(тема.ПутьЭффект);
                ЭффектВидео.Position = TimeSpan.Zero;
                ЭффектВидео.Play();
            }
            else
            {
                ЗаписатьЛогТемы($"fg.mp4 темы «{тема.Id}» не найден: {тема.ПутьЭффект}");
            }

            // ---- лого "СЕРЬЁЗНЫЙ" ----
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

        // OpacityMask в WPF смотрит на АЛЬФА-канал кисти, а не на яркость
        // пикселей. Обычный "плоский" PNG (сохранённый без реальной
        // прозрачности) имеет альфа=255 везде — в том числе у чёрного
        // фона. Из-за этого маска ничего не вырезала, и вся фигура
        // заливалась ЛогоЦвет сплошным цветом — это и был красный
        // квадрат. Здесь вручную превращаем близкие к чёрному пиксели
        // в alpha=0, не трогая сам файл на диске.
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

        // Видео зациклено самим файлом (первый и последний кадр совпадают),
        // поэтому просто перематываем в начало и играем заново.
        private void ФонВидео_MediaEnded(object sender, RoutedEventArgs e)
        {
            ФонВидео.Position = TimeSpan.Zero;
            ФонВидео.Play();
        }

        private void ЭффектВидео_MediaEnded(object sender, RoutedEventArgs e)
        {
            ЭффектВидео.Position = TimeSpan.Zero;
            ЭффектВидео.Play();
        }

        // Ошибка декодирования (нет кодека — частый случай на "голых"
        // сборках Windows без Media Feature Pack) раньше проглатывалась
        // полностью молча. Теперь пишем в тот же club-screen-crash.log.
        private void ФонВидео_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ЗаписатьЛогМедиа("ФонВидео", e.ErrorException);
        }

        private void ЭффектВидео_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ЗаписатьЛогМедиа("ЭффектВидео", e.ErrorException);
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
            if (FindName("ОгненнаяРамкаКисть") is not SolidColorBrush кисть)
                return;

            var анимация = new ColorAnimation
            {
                From = Color.FromRgb(0x5C, 0x18, 0x26), // "спокойное" состояние — тёмно-бордовый
                To = Color.FromRgb(0xFF, 0x3D, 0x66),   // "вспышка" — ярко-красный
                Duration = TimeSpan.FromSeconds(1.8),   // скорость переливания
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

            // Берём название карточки ПК из той же таблицы Computers,
            // что видно в админке под "⚙ Настройка ПК". Запрос идёт
            // вживую каждый раз — удаление/пересоздание карточки с этим
            // Id подхватится автоматически без правок кода.
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

            Hide();

            try
            {
                var окноИгрока = new ОкноИгрока(accountId, pcId);
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
        // ФОРМАТИРОВАНИЕ ТЕЛЕФОНА (без изменений)
        // =====================================================

        private void ПолеТелефон_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (идётФорматированиеТелефона)
                return;

            идётФорматированиеТелефона = true;

            try
            {
                var курсорВКонце = ПолеТелефон.CaretIndex == ПолеТелефон.Text.Length;
                var цифры = ИзвлечьЦифрыСПрефиксом(ПолеТелефон.Text);
                var отформатировано = ФорматТелефона(цифры);

                ПолеТелефон.Text = отформатировано;

                ПолеТелефон.CaretIndex = курсорВКонце
                    ? отформатировано.Length
                    : Math.Min(ПолеТелефон.CaretIndex, отформатировано.Length);

                // Пример формата ("+7 (___) ___-__-__") показываем только
                // пока поле реально пустое — как только появились цифры,
                // прячем подсказку, чтобы она не наезжала на введённый текст.
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

        private static string ИзвлечьЦифрыСПрефиксом(string текст)
        {
            var цифры = new string((текст ?? string.Empty).Where(char.IsDigit).ToArray());

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

                КнопкаПоказатьПароль.Content = "🙈"; // "скрыть"
            }
            else
            {
                ПолеПароль.Password = ПолеПарольВидимый.Text;

                ПолеПарольВидимый.Visibility = Visibility.Collapsed;
                ПолеПароль.Visibility = Visibility.Visible;

                КнопкаПоказатьПароль.Content = "👁"; // "показать"
            }
        }

        // Возвращает актуальный пароль независимо от того, какое из
        // двух полей сейчас видно пользователю.
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

            var телефон = ИзвлечьЦифрыСПрефиксом(ПолеТелефон.Text);
            var пароль = ПолучитьТекущийПароль();

            if (телефон.Length != 11)
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
                var requestId = AccountLoginBridgeService.CreateRequest(телефон, пароль);

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
    }
}