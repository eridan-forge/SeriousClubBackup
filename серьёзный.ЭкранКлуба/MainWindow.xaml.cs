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

namespace серьёзный.ЭкранКлуба
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer таймерЧасов = new();
        private readonly DispatcherTimer наблюдение = new();

      

        private Config config = new();
        private State state = new();

        private bool explorerЗапущен;
        private bool прошлоеСостояние = true;

        private bool окноИгрокаАктивно;

        private bool ЭтоПервыйЗапускShell =>
            (Application.Current as App)?.ЭтоПервыйЗапускShell == true;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += ПриЗагрузке;
            Closing += (_, e) => e.Cancel = true;
        }

        private void ПриЗагрузке(object sender, RoutedEventArgs e)
        {
            ОбновитьНастройки();
            ЗапуститьПатруль();

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
        }

        private void ЗапуститьПатруль()
        {
            try
            {
                if (Process.GetProcessesByName("серьёзный.Патруль").Length > 0)
                    return;

                var путь = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "серьёзный.Патруль.exe");

                if (!File.Exists(путь))
                    return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = путь,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
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

        private void Войти_Click(object sender, RoutedEventArgs e)
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

            АккаунтИгрока? аккаунт =
                new СервисАккаунтов().Авторизовать(имя, пароль);

            if (аккаунт == null)
            {
                ПоказатьОшибку("Неверное имя или пароль.");
                ПолеПароль.Clear();
                return;
            }

            if (аккаунт.ОсталосьВремени <= TimeSpan.Zero)
            {
                ПоказатьОшибку("На аккаунте закончилось время.");
                return;
            }

            ПолеПароль.Clear();

            ОткрытьОкноИгрока(аккаунт.Id, state.PcId);
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

                    // Пока просто сохраняем каталог в кэш.
                    GameCacheService.Store(pcId, games);

                    // Позже здесь будет автоматическое
                    // обновление карточек без перезапуска.
                });
            });
        }
    }
}