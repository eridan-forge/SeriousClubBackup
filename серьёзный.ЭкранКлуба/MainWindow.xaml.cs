using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
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

        private bool ЭтоПервыйЗапускShell =>
            (Application.Current as App)?.ЭтоПервыйЗапускShell == true;

        public MainWindow()
        {
            // Максимально рано — ещё до InitializeComponent, не дожидаясь
            // ни Loaded, ни разблокировки/запуска explorer.exe.
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

            // Watchdog Патруля — отдельный, более редкий таймер. Держать
            // проверку процесса на 250мс-таймере избыточно дорого,
            // Process.GetProcessesByName — не бесплатный вызов.
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

        private async void Войти_Click(object sender, RoutedEventArgs e)
        {
            ТекстОшибка.Visibility = Visibility.Collapsed;

            var имя = ПолеИмя.Text.Trim();
            var пароль = ПолеПароль.Password.Trim();

            if (string.IsNullOrWhiteSpace(имя))
            {
                ПоказатьОшибку("Введите имя.");
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
                 AccountLoginBridgeService.CreateRequest(имя, пароль);

            LoginRequestRecord? результат = null;

            for (int i = 0; i < 100; i++) // до ~10 секунд ожидания сервера
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
                ПоказатьОшибку(результат.Error ?? "Неверное имя или пароль.");
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