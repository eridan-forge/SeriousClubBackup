using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using серьёзный.Модели;
using серьёзный.Сервисы;
using серьёзный.ЭкранКлуба.Модели;
using серьёзный.ЭкранКлуба.Сервисы;
using System.Text.Json;
using серьёзный.Core.CoreServices;
using System.Threading.Tasks;


namespace серьёзный.ЭкранКлуба
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer таймерЧасов = new();
        private readonly DispatcherTimer наблюдение = new();

        private readonly DispatcherTimer патрульНаблюдение = new();

        private Config config = new();
        private State state = new();

        private bool explorerЗапущен;
        private bool прошлоеСостояние = true;

        private bool окноИгрокаАктивно;

        private bool идётФорматированиеТелефона;

        private bool ЭтоПервыйЗапускShell =>
            (Application.Current as App)?.ЭтоПервыйЗапускShell == true;

        public MainWindow()
        {
            // Максимально рано — ещё до InitializeComponent, не дожидаясь
            // ни Loaded, ни разблокировки/запуска explorer.exe. Патруль —
            // самостоятельный процесс, не зависящий от explorer.exe: он
            // подключается к серверу по TCP независимо от того, показан
            // сейчас экран входа или уже открыт рабочий стол.
            PatrolProcessLauncher.ЗапуститьЕслиНужно();

            InitializeComponent();

            Loaded += ПриЗагрузке;
            Closing += (_, e) => e.Cancel = true;
        }

        private void ПриЗагрузке(object sender, RoutedEventArgs e)
        {
            ОбновитьНастройки();

            ЗапуститьПрослушиваниеПередачи();

            таймерЧасов.Interval = TimeSpan.FromSeconds(1);
            таймерЧасов.Tick += (_, _) =>
            {
                Часы.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            таймерЧасов.Start();

            наблюдение.Interval = TimeSpan.FromMilliseconds(250);
            наблюдение.Tick += (_, _) =>
            {
                try
                {
                    ПроверитьСостояние();
                }
                catch
                {
                }
            };
            наблюдение.Start();

            патрульНаблюдение.Interval = TimeSpan.FromSeconds(8);
            патрульНаблюдение.Tick += (_, _) =>
            {
                try
                {
                    PatrolProcessLauncher.ЗапуститьЕслиНужно();
                }
                catch
                {
                }
            };
            патрульНаблюдение.Start();
        }

        private void ОбновитьНастройки()
        {
            config = ConfigService.Загрузить();
            state = StateService.Загрузить();

            НазваниеКлуба.Text = "Серьёзный";
            НомерПК.Text = $"ПК-{state.PcId}";
            ГлавныйТекст.Text = "Войдите в аккаунт";

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

            if (текущее.AccountId.HasValue &&
                текущее.AccountId.Value != Guid.Empty)
            {
                ОткрытьОкноИгрока(
                    текущее.AccountId.Value,
                    текущее.PcId);
            }
            else
            {
                Разблокировать();
            }
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
                var курсорВКонце =
                    ПолеТелефон.CaretIndex == ПолеТелефон.Text.Length;

                var цифры = ИзвлечьЦифрыСПрефиксом(ПолеТелефон.Text);

                var отформатировано = ФорматТелефона(цифры);

                ПолеТелефон.Text = отформатировано;

                ПолеТелефон.CaretIndex = курсорВКонце
                    ? отформатировано.Length
                    : Math.Min(ПолеТелефон.CaretIndex, отформатировано.Length);
            }
            finally
            {
                идётФорматированиеТелефона = false;
            }
        }

        // Приводит к "7ХХХХХХХХХХ" независимо от того, начал человек
        // с 8, с 9 (без кода страны) или с +7.
        private static string ИзвлечьЦифрыСПрефиксом(string текст)
        {
            var цифры =
                new string((текст ?? string.Empty).Where(char.IsDigit).ToArray());

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
        // ВХОД
        // =====================================================

        private async void Войти_Click(object sender, RoutedEventArgs e)
        {
            ТекстОшибка.Visibility = Visibility.Collapsed;

            var телефон = ИзвлечьЦифрыСПрефиксом(ПолеТелефон.Text);
            var пароль = ПолеПароль.Password.Trim();

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
            ТекстОшибка.Foreground = System.Windows.Media.Brushes.LightGray;

            var requestId =
                 AccountLoginBridgeService.CreateRequest(телефон, пароль);

            LoginRequestRecord? результат = null;

            // ~15 секунд — с запасом на случай, если Патруль на этом ПК
            // только что стартовал вместе с экраном входа и ещё
            // договаривается о подключении с сервером.
            for (int i = 0; i < 150; i++)
            {
                await Task.Delay(100);

                результат = AccountLoginBridgeService.GetResult(requestId);

                if (результат != null &&
                       результат.Status != LoginRequestStatus.Pending)
                {
                    break;
                }
            }

            КнопкаВойти.IsEnabled = true;
            ТекстОшибка.Foreground = System.Windows.Media.Brushes.Red;

            if (результат == null ||
                результат.Status == LoginRequestStatus.Pending)
            {
                ПоказатьОшибку("Сервер не ответил. Попробуйте ещё раз.");
                ПолеПароль.Clear();
                return;
            }

            if (результат.Status == LoginRequestStatus.Failed ||
                 !результат.AccountId.HasValue)
            {
                ПоказатьОшибку(результат.Error ?? "Неверный номер телефона или пароль.");
                ПолеПароль.Clear();
                return;
            }

            ТекстОшибка.Visibility = Visibility.Collapsed;
            ПолеПароль.Clear();

            ОткрытьОкноИгрока(результат.AccountId.Value, state.PcId);

        }

        private void ПоказатьОшибку(string текст)
        {
            ТекстОшибка.Text = текст;
            ТекстОшибка.Visibility = Visibility.Visible;
        }

        private void Обслуживание_Click(object sender, RoutedEventArgs e)
        {
            config = ConfigService.Загрузить();

            var окно = new PasswordWindow(config.Password)
            {
                Owner = this,
                Topmost = true
            };

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