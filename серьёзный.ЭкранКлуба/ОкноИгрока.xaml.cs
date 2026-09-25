using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreLaunch;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreProfiles;
using серьёзный.Core.CoreServices;
using серьёзный.Core.CoreShop;
using серьёзный.ЭкранКлуба.Карусель;
using серьёзный.Карточки;
using серьёзный.Модели;
using серьёзный.Окна;
using серьёзный.ЭкранКлуба.Модели;
using серьёзный.ЭкранКлуба.Сервисы;

namespace серьёзный.ЭкранКлуба
{
    public partial class ОкноИгрока : Window
    {
        private readonly DispatcherTimer таймер = new();
        private readonly DispatcherTimer обновление = new();
        private readonly DispatcherTimer игрыОбновление = new() { Interval = TimeSpan.FromSeconds(15) };
        private readonly DispatcherTimer магазинОбновление = new() { Interval = TimeSpan.FromSeconds(5) };
        private readonly DispatcherTimer чатОбновление = new() { Interval = TimeSpan.FromSeconds(3) };

        private readonly GameSessionTracker трекер = new();
        private readonly СервисНастроекИгрока сервисНастроек = new();

        private readonly Guid аккаунтId;
        private readonly int компьютерId;

        private АккаунтИгрока? аккаунт;
        private НастройкиИгрока настройкиИгрока = new();
        private List<Игра> игры = new();
        private string? выбраннаяКатегорияЦеликом;

        private EconomySummaryDto? сводкаЭкономики;
        private PlayerProfileDto? профильДанные;
        private SocialStateDto? социальноеСостояние;

        private bool каталогИгрЗагружается;
        private bool каталогЗагружается;
        private bool чатОткрыт;
        private int показаноСообщенийЧата;

        private bool окноЗакрывается;
        private bool допустимоеЗакрытие;

        private DispatcherTimer? таймерПлашкиОтмены;
        private Игра? последняяСкрытаяИгра;

        // Момент открытия окна для тестового аккаунта — от него локально
        // отсчитывается убывающее время, без обращений к серверу.
        private DateTime? тестовыйСтарт;

        public ОкноИгрока(Guid idАккаунта, int idПК)
        {
            InitializeComponent();

            аккаунтId = idАккаунта;
            компьютерId = idПК;

            КнопкаГлавная.Click += (_, _) => ПоказатьГлавную();
            КнопкаИгры.Click += (_, _) => { ПоказатьИгры(); _ = ЗагрузитьКаталогИгр(); };
            КнопкаМагазин.Click += (_, _) => ПоказатьМагазин();
            КнопкаРазвлечения.Click += (_, _) => ПоказатьРазвлечения();
            КнопкаПрофиль.Click += (_, _) => ПоказатьПрофиль();
            КнопкаИгроки.Click += (_, _) => ПоказатьИгроков();

            MouseLeftButtonDown += ПеретаскиваниеОкна;

            Loaded += ПриЗагрузке;
            Closed += ПриЗакрытии;

            Closing += (_, e) =>
            {
                if (допустимоеЗакрытие)
                    return;

                e.Cancel = true;
                WindowState = WindowState.Minimized;
            };

            чатОбновление.Tick += (_, _) => { if (чатОткрыт) ЗагрузитьИсториюЧата(); };
            чатОбновление.Start();
        }

        // =====================================================
        // ЗАГРУЗКА / ЗАКРЫТИЕ
        // =====================================================

        private async void ПриЗагрузке(object? sender, RoutedEventArgs e)
        {
            аккаунт = await ЗапроситьАккаунтЧерезСерверAsync();

            if (аккаунт == null)
            {
                допустимоеЗакрытие = true;
                Close();
                return;
            }

            ИмяИгрока.Text = аккаунт.ПолноеИмя;
            ТекстПК.Text = $"ПК-{компьютерId}";

            настройкиИгрока = сервисНастроек.Загрузить(аккаунт.Id);

            

            _ = ЗагрузитьКаталогМагазина();
            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.GetSummary });

            игрыОбновление.Tick += (_, _) => _ = ЗагрузитьКаталогИгр();
            игрыОбновление.Start();

            магазинОбновление.Tick += (_, _) => _ = ЗагрузитьКаталогМагазина();
            магазинОбновление.Start();

            таймер.Interval = TimeSpan.FromSeconds(1);
            таймер.Tick += Таймер;
            таймер.Start();

            обновление.Interval = TimeSpan.FromSeconds(2);
            обновление.Tick += ОбновлениеАккаунта;
            обновление.Start();

            ОбновитьИнформацию();
            ПоказатьГлавную();

            LiveGameSync.Refresh += ОбновитьКарточки;
        }

        private void ОбновитьКарточки(int pc)
        {
            if (pc != компьютерId)
                return;

            Dispatcher.Invoke(() => _ = ЗагрузитьКаталогИгр());
        }

        // =====================================================
        // ЗАПУСК / ИЗБРАННОЕ / СКРЫТИЕ ИГР
        // =====================================================

        private void ЗапускИгры(Игра игра)
        {
            if (окноЗакрывается || string.IsNullOrWhiteSpace(игра.Путь))
                return;

            try
            {
                if (игра.Путь.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = игра.Путь,
                        UseShellExecute = true
                    });

                    return;
                }

                if (!File.Exists(игра.Путь))
                {
                    MessageBox.Show("Файл игры не найден.");
                    return;
                }

                var процесс = Process.Start(new ProcessStartInfo
                {
                    FileName = игра.Путь,
                    WorkingDirectory = Path.GetDirectoryName(игра.Путь),
                    UseShellExecute = true
                });

                трекер.Finished += прошло =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        GameSessionReportBridgeService.CreateRequest(
                            аккаунтId,
                            компьютерId,
                            (long)прошло.TotalSeconds);
                    });
                };

                трекер.Start(процесс);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ПереключитьИзбранное(Игра игра)
        {
            if (настройкиИгрока.Избранное.Contains(игра.Id))
                настройкиИгрока.Избранное.Remove(игра.Id);
            else
                настройкиИгрока.Избранное.Add(игра.Id);

            сервисНастроек.Сохранить(настройкиИгрока);

            ОбновитьСетку();
        }

        private void СкрытьИгру(Игра игра)
        {
            if (настройкиИгрока.Скрытые.Contains(игра.Id))
                return;

            настройкиИгрока.Скрытые.Add(игра.Id);
            сервисНастроек.Сохранить(настройкиИгрока);

            последняяСкрытаяИгра = игра;

            ТекстПлашкиОтмены.Text = $"«{игра.Название}» скрыта";
            ПлашкаОтменыСкрытия.Visibility = Visibility.Visible;

            таймерПлашкиОтмены?.Stop();

            таймерПлашкиОтмены = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            таймерПлашкиОтмены.Tick += (_, _) =>
            {
                таймерПлашкиОтмены?.Stop();
                ПлашкаОтменыСкрытия.Visibility = Visibility.Collapsed;
            };

            таймерПлашкиОтмены.Start();

            ОбновитьСетку();
        }

        private void ОтменитьСкрытие_Click(object sender, RoutedEventArgs e)
        {
            таймерПлашкиОтмены?.Stop();
            ПлашкаОтменыСкрытия.Visibility = Visibility.Collapsed;

            if (последняяСкрытаяИгра == null)
                return;

            настройкиИгрока.Скрытые.Remove(последняяСкрытаяИгра.Id);
            сервисНастроек.Сохранить(настройкиИгрока);

            последняяСкрытаяИгра = null;

            ОбновитьСетку();
        }

        private void ПриЗакрытии(object? sender, EventArgs e)
        {
            окноЗакрывается = true;

            таймер.Stop();
            обновление.Stop();
            игрыОбновление.Stop();
            магазинОбновление.Stop();
            чатОбновление.Stop();
            таймерПлашкиОтмены?.Stop();

            LiveGameSync.Refresh -= ОбновитьКарточки;
        }

        private void ПеретаскиваниеОкна(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void ВыбратьИгру_Click(object sender, RoutedEventArgs e)
        {
            ПоказатьИгры();
            _ = ЗагрузитьКаталогИгр();
        }

        private void ВернутьсяКРабочемуСтолу_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

private async Task<АккаунтИгрока?> ЗапроситьАккаунтЧерезСерверAsync()
        {

            // Тестовый аккаунт — единственный захардкоженный, вводится на
                        // экране входа в обход сервера/Патруля (см. Войти_Click в
                        // MainWindow ЭкранКлуба). Не делает никакого сетевого запроса.
                        if (аккаунтId == ТестовыйАккаунт.Id)
                                return ПостроитьТестовыйАккаунт();

            var requestId = AccountBalanceBridgeService.CreateRequest(аккаунтId);

            BalanceRequestRecord? результат = null;

            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(200);
                результат = AccountBalanceBridgeService.GetResult(requestId);
                if (результат != null && результат.Done) break;
            }

            if (результат == null || !результат.Done || результат.Failed)
                return аккаунт;

            var базовый = аккаунт ?? new АккаунтИгрока { Id = аккаунтId };

            базовый.ОсталосьВремени = TimeSpan.FromSeconds(результат.RemainingSeconds);
            базовый.ВсегоСыграно = TimeSpan.FromSeconds(результат.PlayedSeconds);
            базовый.ВсегоСеансов = результат.SessionCount;

            return базовый;
        }

         // Локальные тестовые данные: время убывает от НачальныйОстаток 
        // с момента первого открытия окна этим аккаунтом — по дошествии
        // нуля Таймер сам вызовет ЗакрытьОкно(), как у настоящей сессии.
        private АккаунтИгрока ПостроитьТестовыйАккаунт()
        {
            тестовыйСтарт ??= DateTime.Now;

            var прошло = DateTime.Now - тестовыйСтарт.Value;

           var осталось = ТестовыйАккаунт.НачальныйОстаток - прошло;

            if (осталось<TimeSpan.Zero)
                осталось = TimeSpan.Zero;

            return new АккаунтИгрока
           {
                Id = ТестовыйАккаунт.Id,
                Имя = ТестовыйАккаунт.Имя,
                Телефон = ТестовыйАккаунт.Телефон,
                ОсталосьВремени = осталось,
                ВсегоСыграно = прошло,
                ВсегоСеансов = 1
            };
       }

// =====================================================
// ТАЙМЕРЫ
// =====================================================

private void Таймер(object? sender, EventArgs e)
        {
            if (окноЗакрывается) return;

            ОбновитьСетку();
            ОбновитьИнформацию();
            ПроверитьДостижения();

            if (аккаунт != null && аккаунт.ОсталосьВремени <= TimeSpan.Zero)
                ЗакрытьОкно();
        }

        private async void ОбновлениеАккаунта(object? sender, EventArgs e)
        {
            if (окноЗакрывается) return;

            if (НужноЗакрытьОкно())
            {
                ЗакрытьОкно();
                return;
            }

            аккаунт = await ЗапроситьАккаунтЧерезСерверAsync();

            if (аккаунт == null)
            {
                ЗакрытьОкно();
                return;
            }

            ОбновитьИнформацию();
        }

        private bool НужноЗакрытьОкно()
        {
            try
            {
                var состояние = StateService.Загрузить();

                if (состояние.Locked) return true;
                if (!состояние.AccountId.HasValue) return true;

                return состояние.AccountId.Value != аккаунтId;
            }
            catch
            {
                return false;
            }
        }

        private void ЗакрытьОкно()
        {
            if (окноЗакрывается) return;

            окноЗакрывается = true;

            таймер.Stop();
            обновление.Stop();
            игрыОбновление.Stop();
            магазинОбновление.Stop();
            чатОбновление.Stop();

            допустимоеЗакрытие = true;

            WindowState = WindowState.Minimized;
            Close();
        }

        private void ОбновитьИнформацию()
        {
            if (аккаунт == null) return;

            var время = аккаунт.ОсталосьВремени;
            if (время < TimeSpan.Zero) время = TimeSpan.Zero;

            ТекстОсталосьКратко.Text = ФорматСекунды(время);
            БольшойТаймер.Text = ФорматСекунды(время);
            ТекстСыграно.Text = ФорматКороткий(аккаунт.ВсегоСыграно);
            ТекстСеансов.Text = аккаунт.ВсегоСеансов.ToString();

            if (время.TotalMinutes <= 5)
            {
                ТекстСтатус.Text = "Мало времени";
                ТекстСтатус.Foreground = (Brush)FindResource("Опасность");
            }
            else
            {
                ТекстСтатус.Text = "Играет";
                ТекстСтатус.Foreground = (Brush)FindResource("Успех");
            }
        }

        private void ПроверитьДостижения()
        {
            try
            {
                var уведомление = AchievementNotificationBridgeService.TakeNextPending(аккаунтId);
                if (уведомление == null) return;

                AchievementNotificationBridgeService.MarkDelivered(уведомление.Id);

                new серьёзный.ЭкранКлуба.Уведомления.AchievementToast(уведомление.Name, уведомление.Description).Show();

                if (СтраницаПрофиль.Visibility == Visibility.Visible)
                    _ = ЗагрузитьПрофильAsync();
            }
            catch
            {
            }
        }

        // =====================================================
        // НАВИГАЦИЯ
        // =====================================================

        private void СкрытьВсеСтраницы()
        {
            СтраницаГлавная.Visibility = Visibility.Collapsed;
            СтраницаИгры.Visibility = Visibility.Collapsed;
            СтраницаМагазин.Visibility = Visibility.Collapsed;
            СтраницаРазвлечения.Visibility = Visibility.Collapsed;
            СтраницаПрофиль.Visibility = Visibility.Collapsed;
            СтраницаИгроки.Visibility = Visibility.Collapsed;
        }

        private void Выделить(Button активная)
        {
            foreach (var b in new[] { КнопкаГлавная, КнопкаИгры, КнопкаМагазин, КнопкаРазвлечения, КнопкаПрофиль, КнопкаИгроки })
                b.Tag = null;

            активная.Tag = "Активна";
        }

        private void ПоказатьГлавную()
        {
            СкрытьВсеСтраницы();
            СтраницаГлавная.Visibility = Visibility.Visible;
            Выделить(КнопкаГлавная);
        }

        private void ПоказатьИгры()
        {
            СкрытьВсеСтраницы();
            СтраницаИгры.Visibility = Visibility.Visible;
            Выделить(КнопкаИгры);
        }

        private void ПоказатьМагазин()
        {
            СкрытьВсеСтраницы();
            СтраницаМагазин.Visibility = Visibility.Visible;
            Выделить(КнопкаМагазин);
           
        }

        private void ПоказатьРазвлечения()
        {
            СкрытьВсеСтраницы();
            СтраницаРазвлечения.Visibility = Visibility.Visible;
            Выделить(КнопкаРазвлечения);

            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.GetSummary });
        }

        private void ПоказатьПрофиль()
        {
            СкрытьВсеСтраницы();
            СтраницаПрофиль.Visibility = Visibility.Visible;
            Выделить(КнопкаПрофиль);

            _ = ЗагрузитьПрофильAsync();
            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.GetSummary });
        }

        private void ПоказатьИгроков()
        {
            СкрытьВсеСтраницы();
            СтраницаИгроки.Visibility = Visibility.Visible;
            Выделить(КнопкаИгроки);

            _ = ОбновитьИгроковAsync(new SocialActionDto { Action = SocialAction.GetState });
        }

        // =====================================================
        // БЫСТРЫЕ ДЕЙСТВИЯ: ПРОДЛИТЬ / ЗАВЕРШИТЬ / ОШИБКА
        // =====================================================

        private void Продлить_Click(object sender, RoutedEventArgs e)
        {
            var окно = new ОкноВвода("На сколько минут продлить?", "30") { Owner = this };

            if (окно.ShowDialog() != true) return;

            if (!int.TryParse(окно.Текст.Trim(), out var минуты) || минуты <= 0)
            {
                MessageBox.Show("Введите положительное число минут.");
                return;
            }

            _ = ПродлитьAsync(минуты);
        }

        private void БыстроеПродление_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && int.TryParse(b.Tag?.ToString(), out var минуты))
                _ = ПродлитьAsync(минуты);
        }

        private async Task ПродлитьAsync(int минуты)
        {
            await ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.ExtendSession, ExtendMinutes = минуты });

            аккаунт = await ЗапроситьАккаунтЧерезСерверAsync();
            ОбновитьИнформацию();
        }

        private void ЗавершитьСеанс_Click(object sender, RoutedEventArgs e)
        {
            var ответ = MessageBox.Show(
                "Завершить сеанс? Оставшееся время нельзя будет продолжить на этом ПК.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ответ != MessageBoxResult.Yes) return;

            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.EndSession });
        }

        private void СообщитьОбОшибке_Click(object sender, RoutedEventArgs e)
        {
            var окно = new ОкноВвода("Опишите проблему (необязательно)", "") { Owner = this };

            if (окно.ShowDialog() != true) return;

            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.ReportIssue, IssueText = окно.Текст });
        }

        // =====================================================
        // ЭКОНОМИКА (баллы, казино, кейсы, инвентарь, тарифы, продление/завершение/сигнал)
        // =====================================================

        private async Task ОбновитьЭкономикуAsync(EconomyRequestDto запрос)
        {
            var requestId = EconomyBridgeService.CreateRequest(аккаунтId, запрос);

            EconomyResultDto? результат = null;

            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(200);
                результат = EconomyBridgeService.GetResult(requestId);
                if (результат != null) break;
            }

            if (результат == null)
            {
                if (запрос.Action != EconomyAction.GetSummary)
                    MessageBox.Show("Сервер не ответил. Попробуйте ещё раз.", "Серьёзный");
                return;
            }

            if (!результат.Success)
            {
                if (запрос.Action != EconomyAction.GetSummary)
                    MessageBox.Show(результат.Error ?? "Ошибка.", "Серьёзный");
                return;
            }

            if (результат.RewardLabel != null)
                MessageBox.Show($"🎉 Выпало: {результат.RewardLabel}", "Кейс открыт");

            if (запрос.Action == EconomyAction.PlayCasino)
            {
                MessageBox.Show(
                    результат.Win ? $"🎉 Выигрыш! +{результат.Payout} баллов" : "😔 Не повезло. Попробуй ещё раз!",
                    "Казино");
            }

            if (запрос.Action == EconomyAction.ReportIssue)
                MessageBox.Show("Администратор получил сигнал и скоро подойдёт.", "Отправлено");

            if (результат.SessionEnded)
            {
                ЗакрытьОкно();
                return;
            }

            сводкаЭкономики = результат.Summary;

            if (сводкаЭкономики != null)
                ОтрисоватьЭкономику();
        }

        private void ОтрисоватьЭкономику()
        {
            if (сводкаЭкономики == null) return;

            БлокБаллов.Visibility = сводкаЭкономики.ShowPoints ? Visibility.Visible : Visibility.Collapsed;
            ТекстБаллыКратко.Text = $"⭐ {сводкаЭкономики.Points}";
            БлокПремиум.Visibility = сводкаЭкономики.Premium ? Visibility.Visible : Visibility.Collapsed;

            ПостроитьТарифыГлавная();
            ПостроитьКейсы();

            ПолеСтавки.IsEnabled = сводкаЭкономики.CasinoEnabled;

            ТекстПравилаКазино.Text = сводкаЭкономики.CasinoEnabled
                ? $"Ставка от {сводкаЭкономики.CasinoMinBet} до {сводкаЭкономики.CasinoMaxBet} баллов."
                : "Казино сейчас отключено администратором.";

            if (профильДанные != null)
            {
                БаллыПрофиль.Visibility = сводкаЭкономики.ShowPoints ? Visibility.Visible : Visibility.Collapsed;
                ПостроитьИнвентарьПрофиль();
            }
        }

        private void ПостроитьТарифыГлавная()
        {
            ПанельТарифыГлавная.Children.Clear();

            foreach (var t in сводкаЭкономики!.Tariffs)
            {
                var стек = new StackPanel();

                стек.Children.Add(new TextBlock { Text = t.Label, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 15 });

                if (сводкаЭкономики.ShowSessionCost)
                {
                    стек.Children.Add(new TextBlock
                    {
                        Text = $"{t.Price:0} ₽",
                        Foreground = (Brush)FindResource("Золото"),
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }

                ПанельТарифыГлавная.Children.Add(new Border
                {
                    Style = (Style)FindResource("КарточкаМалая"),
                    Width = 170,
                    Margin = new Thickness(0, 0, 12, 12),
                    Child = стек
                });
            }
        }

        private void ПостроитьКейсы()
        {
            ПанельКейсов.Children.Clear();

            foreach (var кейс in сводкаЭкономики!.Cases)
            {
                var кнопка = new Button
                {
                    Content = $"{кейс.Icon} {кейс.Name}\n{кейс.PriceInPoints} баллов",
                    Width = 170,
                    Height = 74,
                    Margin = new Thickness(0, 0, 12, 12)
                };

                кнопка.Click += (_, _) => _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.OpenCase, CaseId = кейс.Id });

                ПанельКейсов.Children.Add(кнопка);
            }
        }

        private void Крутить_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(ПолеСтавки.Text, out var ставка) || ставка <= 0)
            {
                MessageBox.Show("Введите корректную ставку.");
                return;
            }

            _ = ОбновитьЭкономикуAsync(new EconomyRequestDto { Action = EconomyAction.PlayCasino, Bet = ставка });
        }

        private void ПостроитьИнвентарьПрофиль()
        {
            ПанельИнвентарьПрофиль.Children.Clear();

            if (сводкаЭкономики == null) return;

            foreach (var item in сводкаЭкономики.Inventory)
            {
                var кнопка = new Button
                {
                    Width = 200,
                    Height = 90,
                    Margin = new Thickness(0, 0, 12, 12),
                    Content =
                        $"{item.Icon} {item.Name}\n" +
                        $"+{item.PointsBonusPercent}% баллов, +{item.TimeBonusPercent}% времени\n" +
                        (item.Equipped ? "✅ Надето" : "Надеть")
                };

                кнопка.Click += (_, _) => _ = ОбновитьЭкономикуAsync(new EconomyRequestDto
                {
                    Action = EconomyAction.SetEquipped,
                    ItemId = item.Id,
                    Equipped = !item.Equipped
                });

                ПанельИнвентарьПрофиль.Children.Add(кнопка);
            }

            if (сводкаЭкономики.Inventory.Count == 0)
            {
                ПанельИнвентарьПрофиль.Children.Add(new TextBlock
                {
                    Text = "Инвентарь пуст.",
                    Foreground = (Brush)FindResource("ТекстВторичный")
                });
            }
        }

        // =====================================================
        // ПРОФИЛЬ (достижения, рамки, уровень — читается с сервера)
        // =====================================================

        private async Task ЗагрузитьПрофильAsync()
        {
            var requestId = PlayerProfileBridgeService.CreateRequest(аккаунтId);

            PlayerProfileDto? результат = null;

            for (int i = 0; i < 25; i++)
            {
                await Task.Delay(250);
                результат = PlayerProfileBridgeService.GetResult(requestId);
                if (результат != null) break;
            }

            if (результат == null) return;

            профильДанные = результат;
            ОтрисоватьПрофиль();
        }

        private void ОтрисоватьПрофиль()
        {
            if (профильДанные == null) return;

            ИмяПрофиль.Text = профильДанные.FullName;
            БукваАватара.Text = профильДанные.FullName.Length > 0 ? профильДанные.FullName.Substring(0, 1).ToUpper() : "?";
            УровеньПрофиль.Text = $"{профильДанные.LevelName} • x{профильДанные.LevelMultiplierPercent / 100.0:0.00}";

            var показыватьБаллы = сводкаЭкономики?.ShowPoints ?? true;
            БаллыПрофиль.Visibility = показыватьБаллы ? Visibility.Visible : Visibility.Collapsed;
            БаллыПрофиль.Text = $"⭐ {профильДанные.Points} баллов";

            ПремиумПрофиль.Text = !профильДанные.Premium
                ? ""
                : профильДанные.PremiumUntil.HasValue
                    ? $"⭐ Премиум до {профильДанные.PremiumUntil.Value:dd.MM.yyyy}"
                    : "⭐ Премиум бессрочно";

            var текущаяРамка = (ProfileFrame)профильДанные.CurrentFrame;
            РамкаАватара.BorderBrush = КистьРамки(текущаяРамка);

            ПостроитьДостиженияПрофиль();
            ПостроитьРамкиПрофиль();
        }

        private void ПостроитьДостиженияПрофиль()
        {
            ПанельДостиженияПрофиль.Children.Clear();

            foreach (var item in профильДанные!.Achievements)
            {
                var icon = item.Unlocked ? "✔" : "🔒";
                var цвет = item.Unlocked ? (Brush)FindResource("Успех") : (Brush)FindResource("ТекстПриглушённый");

                var стек = new StackPanel();

                стек.Children.Add(new TextBlock { Text = $"{icon} {item.Name}", Foreground = цвет, FontWeight = FontWeights.Bold, FontSize = 15 });
                стек.Children.Add(new TextBlock
                {
                    Text = item.Description,
                    Foreground = (Brush)FindResource("ТекстВторичный"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                ПанельДостиженияПрофиль.Children.Add(new Border
                {
                    Style = (Style)FindResource("КарточкаМалая"),
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = стек
                });
            }

            if (профильДанные.Achievements.Count == 0)
            {
                ПанельДостиженияПрофиль.Children.Add(new TextBlock
                {
                    Text = "Достижений пока нет.",
                    Foreground = (Brush)FindResource("ТекстВторичный")
                });
            }
        }

        private void ПостроитьРамкиПрофиль()
        {
            ПанельРамкиПрофиль.Children.Clear();

            foreach (var frameDto in профильДанные!.Frames)
            {
                var frame = (ProfileFrame)frameDto.Frame;

                ПанельРамкиПрофиль.Children.Add(new Button
                {
                    Width = 140,
                    Height = 46,
                    Margin = new Thickness(0, 0, 10, 10),
                    Content = frameDto.Owned ? НазваниеРамки(frame) : $"🔒 {НазваниеРамки(frame)}",
                    IsEnabled = false,
                    BorderThickness = new Thickness(2),
                    BorderBrush = КистьРамки(frame),
                    Opacity = frameDto.Owned ? 1 : 0.4
                });
            }
        }

        private static string НазваниеРамки(ProfileFrame frame) => frame switch
        {
            ProfileFrame.Silver => "Серебро",
            ProfileFrame.Gold => "Золото",
            ProfileFrame.Neon => "Неон",
            ProfileFrame.Legend => "Легенда",
            _ => "Стандарт"
        };

        private Brush КистьРамки(ProfileFrame frame) => frame switch
        {
            ProfileFrame.Silver => new SolidColorBrush(Color.FromRgb(210, 210, 210)),
            ProfileFrame.Gold => new SolidColorBrush(Color.FromRgb(255, 204, 0)),
            ProfileFrame.Neon => new SolidColorBrush(Color.FromRgb(0, 255, 255)),
            ProfileFrame.Legend => new SolidColorBrush(Color.FromRgb(180, 90, 255)),
            _ => (Brush)FindResource("РамкаЦвет")
        };

        // =====================================================
        // ИГРОКИ КЛУБА / ДРУЗЬЯ
        // =====================================================

        private async Task ОбновитьИгроковAsync(SocialActionDto действие)
        {
            var requestId = SocialBridgeService.CreateRequest(аккаунтId, действие);

            SocialStateDto? результат = null;

            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(300);
                результат = SocialBridgeService.GetResult(requestId);
                if (результат != null) break;
            }

            if (результат == null) return;

            социальноеСостояние = результат;
            ОтрисоватьИгроков();
        }

        private void ОтрисоватьИгроков()
        {
            if (социальноеСостояние == null) return;

            СписокИгроковКлуба.Children.Clear();

            if (социальноеСостояние.Incoming.Count > 0)
            {
                СписокИгроковКлуба.Children.Add(new TextBlock
                {
                    Text = $"📨 Заявки в друзья ({социальноеСостояние.Incoming.Count})",
                    Foreground = Brushes.White,
                    FontSize = 17,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                foreach (var заявка in социальноеСостояние.Incoming)
                    СписокИгроковКлуба.Children.Add(СоздатьКарточкуЗаявки(заявка));

                СписокИгроковКлуба.Children.Add(new Border
                {
                    Height = 1,
                    Background = (Brush)FindResource("РамкаЦвет"),
                    Margin = new Thickness(0, 4, 0, 16)
                });
            }

            var поиск = ПоискИгроков.Text.Trim().ToLower();

            foreach (var player in социальноеСостояние.Players)
            {
                if (!string.IsNullOrWhiteSpace(поиск) && !player.FullName.ToLower().Contains(поиск))
                    continue;

                СписокИгроковКлуба.Children.Add(СоздатьКарточкуИгрока(player));
            }
        }

        private UIElement СоздатьКарточкуЗаявки(IncomingFriendRequestDto заявка)
        {
            var принять = new Button { Content = "✅ Принять", Width = 110, Height = 34, Margin = new Thickness(4) };

            принять.Click += (_, _) => _ = ОбновитьИгроковAsync(new SocialActionDto
            {
                Action = SocialAction.AcceptFriendRequest,
                RequestId = заявка.RequestId
            });

            var отклонить = new Button { Content = "✕ Отклонить", Width = 110, Height = 34, Margin = new Thickness(4) };

            отклонить.Click += (_, _) => _ = ОбновитьИгроковAsync(new SocialActionDto
            {
                Action = SocialAction.RemoveFriend,
                TargetId = заявка.FromAccountId
            });

            var кнопки = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            кнопки.Children.Add(принять);
            кнопки.Children.Add(отклонить);

            var строка = new DockPanel();

            строка.Children.Add(new TextBlock
            {
                Text = заявка.FromFullName,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            DockPanel.SetDock(кнопки, Dock.Right);
            строка.Children.Add(кнопки);

            return new Border { Style = (Style)FindResource("КарточкаМалая"), Margin = new Thickness(0, 0, 0, 10), Child = строка };
        }

        private UIElement СоздатьКарточкуИгрока(OnlinePlayerDto player)
        {
            var друг = new Button
            {
                Content = player.IsFriend ? "Удалить" : player.HasPendingOutgoing ? "Заявка отправлена" : "Добавить",
                IsEnabled = player.IsFriend || !player.HasPendingOutgoing,
                Width = 130,
                Margin = new Thickness(4)
            };

            друг.Click += (_, _) => _ = ОбновитьИгроковAsync(new SocialActionDto
            {
                Action = player.IsFriend ? SocialAction.RemoveFriend : SocialAction.SendFriendRequest,
                TargetId = player.AccountId
            });

            var чат = new Button { Content = "💬", Width = 50, Margin = new Thickness(4) };

            чат.Click += (_, _) =>
            {
                new ОкноЛичногоЧатаИгрока(аккаунтId, аккаунт?.ПолноеИмя ?? "Игрок", player.AccountId, player.FullName)
                {
                    Owner = this
                }.Show();
            };

            var avatar = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = (Brush)FindResource("ФонКарточкиАльт"),
                Margin = new Thickness(0, 0, 16, 0),
                Child = new TextBlock
                {
                    Text = player.FullName.Length > 0 ? player.FullName.Substring(0, 1).ToUpper() : "?",
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var info = new StackPanel();

            info.Children.Add(new TextBlock { Text = player.FullName, Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold });
            info.Children.Add(new TextBlock
            {
                Text = player.Online ? $"🟢 ПК-{player.PcId:D2} • {player.CurrentGame ?? "В клубе"}" : "⚫ Не в сети",
                Foreground = player.Online ? (Brush)FindResource("Успех") : (Brush)FindResource("ТекстВторичный")
            });

            var top = new StackPanel { Orientation = Orientation.Horizontal };
            top.Children.Add(avatar);
            top.Children.Add(info);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            buttons.Children.Add(друг);
            buttons.Children.Add(чат);

            return new Border
            {
                Style = (Style)FindResource("КарточкаМалая"),
                Margin = new Thickness(0, 0, 0, 12),
                Cursor = Cursors.Hand,
                Child = new StackPanel { Children = { top, buttons } }
            };
        }

        private void ПоискИгроков_TextChanged(object sender, TextChangedEventArgs e) => ОтрисоватьИгроков();

        // =====================================================
        // ЧАТ С АДМИНИСТРАТОРОМ (выезжающая панель)
        // =====================================================

        private void ПереключитьЧат_Click(object sender, RoutedEventArgs e)
        {
            чатОткрыт = !чатОткрыт;

            var анимация = new DoubleAnimation(чатОткрыт ? 0 : 380, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            СдвигЧата.BeginAnimation(TranslateTransform.XProperty, анимация);

            if (чатОткрыт)
                ЗагрузитьИсториюЧата();
        }

        private async void ЗагрузитьИсториюЧата()
        {
            var requestId = ChatHistoryBridgeService.CreateRequest(компьютерId);

            ChatHistoryDto? результат = null;

            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(300);
                результат = ChatHistoryBridgeService.GetResult(requestId);
                if (результат != null) break;
            }

            if (результат == null) return;
            if (результат.Сообщения.Count == показаноСообщенийЧата) return;

            ИсторияЧата.Children.Clear();

            foreach (var сообщение in результат.Сообщения)
                ДобавитьПузырьЧата(сообщение);

            показаноСообщенийЧата = результат.Сообщения.Count;

            СкроллЧата.ScrollToEnd();
        }

        private void ДобавитьПузырьЧата(ChatMessageDto сообщение)
        {
            var пузырь = new Border
            {
                Background = сообщение.ОтАдминистратора
                    ? new SolidColorBrush(Color.FromRgb(92, 24, 48))
                    : (Brush)FindResource("ФонКарточкиАльт"),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12),
                Margin = new Thickness(сообщение.ОтАдминистратора ? 40 : 0, 4, сообщение.ОтАдминистратора ? 0 : 40, 4),
                HorizontalAlignment = сообщение.ОтАдминистратора ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 280
            };

            var панель = new StackPanel();

            панель.Children.Add(new TextBlock { Text = сообщение.Имя, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            панель.Children.Add(new TextBlock { Text = сообщение.Текст, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
            панель.Children.Add(new TextBlock
            {
                Text = сообщение.Время.ToString("HH:mm"),
                Foreground = Brushes.LightGray,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            пузырь.Child = панель;

            ИсторияЧата.Children.Add(пузырь);
        }

        private void ОтправитьСообщениеЧат_Click(object sender, RoutedEventArgs e)
        {
            var текст = ПолеСообщенияЧат.Text.Trim();
            if (string.IsNullOrWhiteSpace(текст)) return;

            ChatOutboxBridgeService.CreateRequest(компьютерId, аккаунт?.ПолноеИмя ?? "Игрок", текст);

            ПолеСообщенияЧат.Clear();

            ДобавитьПузырьЧата(new ChatMessageDto { Имя = аккаунт?.ПолноеИмя ?? "Игрок", Текст = текст, Время = DateTime.Now, ОтАдминистратора = false });

            показаноСообщенийЧата++;

            СкроллЧата.ScrollToEnd();
        }

        private void ПолеСообщенияЧат_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Keyboard.Modifiers == ModifierKeys.Shift) return;

            e.Handled = true;
            ОтправитьСообщениеЧат_Click(this, new RoutedEventArgs());
        }

        // =====================================================
        // ИГРЫ
        // =====================================================

        private async Task ЗагрузитьКаталогИгр()
        {
            if (окноЗакрывается || каталогИгрЗагружается) return;

            каталогИгрЗагружается = true;

            try
            {
                GameCatalogDto? каталог;
                
                                if (аккаунтId == ТестовыйАккаунт.Id)
                                    {
                                        // Тестовый аккаунт работает в обход сервера/Патруля —
                                        // сетевой запрос каталога никогда не получит ответ,
                                       // поэтому сразу строим пустой каталог локально вместо
                                        // 6 секунд бесполезного ожидания.
                    каталог = new GameCatalogDto();
                                    }
                                else
                                    {
                    var requestId = GameCatalogBridgeService.CreateRequest(компьютерId);
                    
                   каталог = null;
                    
                                        for (int i = 0; i < 20; i++)
                                            {
                        await Task.Delay(300);
                                                if (окноЗакрывается) return;
                        каталог = GameCatalogBridgeService.GetResult(requestId);
                                                if (каталог != null) break;
                                            }
                    
                                       if (каталог == null) return;
                                    }




                // ===================== ВРЕМЕННЫЕ ТЕСТОВЫЕ КАРТОЧКИ =====================
                // УДАЛИТЬ ЭТОТ БЛОК ЦЕЛИКОМ, когда проверишь карусель на одном ПК.
                if (каталог.Games.Count < 8)
                {
                    var тестовыеОбложки = new[]
                    {
        "https://cdn.cloudflare.steamstatic.com/steam/apps/730/library_600x900.jpg",
        "https://cdn.cloudflare.steamstatic.com/steam/apps/570/library_600x900.jpg",
        "https://cdn.cloudflare.steamstatic.com/steam/apps/1174180/library_600x900.jpg",
    };

                    for (int i = 0; i < 8; i++)
                    {
                        каталог.Games.Add(new GameCatalogItemDto
                        {
                            Id = Guid.NewGuid(),
                            Название = $"Тестовая игра {i + 1}",
                            Категория = i % 2 == 0 ? "Шутеры" : "Популярные",
                            Путь = "",
                            Обложка = "",
                            ОбложкаData = null,
                            ОбложкаExtension = null
                        });
                    }
                }
                // ===================== КОНЕЦ ВРЕМЕННОГО БЛОКА =====================

                игры = каталог.Games
                    .Select(x => new Игра
                    {
                        Id = x.Id,
                        Название = x.Название,
                        Категория = x.Категория,
                        Описание = x.Описание,
                        Путь = x.Путь,
                        Обложка = ImageCacheService.SaveIfNeeded($"game-{x.Id}", x.ОбложкаData, x.ОбложкаExtension) ?? x.Обложка,
                        Порядок = x.Порядок,
                        AppId = x.AppId,
                        Launcher = x.Launcher
                    })
                    .ToList();

                ОбновитьСетку();
            }
            finally
            {
                каталогИгрЗагружается = false;
            }
        }

        private void ОбновитьСетку()
        {
            var поиск = ПоискИгр.Text.Trim();

            var видимые = игры
                .Where(x => !настройкиИгрока.Скрытые.Contains(x.Id))
                .ToList();

            var категории = видимые
                .Select(x => x.Категория)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var избранные = настройкиИгрока.Избранное.ToHashSet();

            ПостроитьКнопкиКатегорий(категории, поиск);

            if (!string.IsNullOrWhiteSpace(поиск))
            {
                var найденные = видимые
                    .Where(x => x.Название.Contains(поиск, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Название)
                    .ToList();

                ПоказатьСеткуКатегории($"Поиск: «{поиск}»", найденные, избранные);
                return;
            }

            if (выбраннаяКатегорияЦеликом != null)
            {
                var игрыКатегории = видимые
                    .Where(x => x.Категория == выбраннаяКатегорияЦеликом)
                    .OrderBy(x => x.Название)
                    .ToList();

                if (игрыКатегории.Count > 0)
                {
                    ПоказатьСеткуКатегории(выбраннаяКатегорияЦеликом, игрыКатегории, избранные);
                    return;
                }

                выбраннаяКатегорияЦеликом = null;
            }

            ПоказатьОбзорПоКатегориям(видимые, категории, избранные);
        }

        // =====================================================
        // КНОПКИ КАТЕГОРИЙ (слева)
        // =====================================================

        private void ПостроитьКнопкиКатегорий(List<string> категории, string поиск)
        {
            ПанельКнопокКатегорий.Children.Clear();

            ПанельКнопокКатегорий.Children.Add(
                СоздатьКнопкуКатегории("Все категории", null, поиск));

            foreach (var категория in категории)
                ПанельКнопокКатегорий.Children.Add(
                    СоздатьКнопкуКатегории(категория, категория, поиск));
        }

        private Button СоздатьКнопкуКатегории(string подпись, string? категория, string поиск)
        {
            bool идётПоиск = !string.IsNullOrWhiteSpace(поиск);

            bool активна = !идётПоиск && категория == выбраннаяКатегорияЦеликом;

            var кнопка = new Button
            {
                Content = подпись,
                Height = 46,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 0, 14, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = активна ? (Brush)FindResource("АкцентКрасный") : (Brush)FindResource("ФонКарточкиАльт"),
                Foreground = Brushes.White,
                BorderBrush = активна ? (Brush)FindResource("АкцентКрасный") : (Brush)FindResource("РамкаЦвет"),
                FontWeight = активна ? FontWeights.Bold : FontWeights.Normal,
                Cursor = Cursors.Hand
            };

            кнопка.Click += (_, _) =>
            {
                выбраннаяКатегорияЦеликом = категория;

                if (ПоискИгр.Text.Length > 0)
                    ПоискИгр.Text = string.Empty; // само вызовет ОбновитьСетку()
                else
                    ОбновитьСетку();
            };

            return кнопка;
        }

        // =====================================================
        // ОБЗОР: КАРУСЕЛЬ НА КАЖДУЮ КАТЕГОРИЮ, ВНИЗ ПОДРЯД
        // =====================================================

        private void ПоказатьОбзорПоКатегориям(List<Игра> видимые, List<string> категории, HashSet<Guid> избранные)
        {
            ПанельОднойКатегории.Visibility = Visibility.Collapsed;
            СкроллОбзораИгр.Visibility = Visibility.Visible;

            ПанельСекцийКатегорий.Children.Clear();

            foreach (var категория in категории)
            {
                var игрыКатегории = видимые
                    .Where(x => x.Категория == категория)
                    .OrderBy(x => x.Название)
                    .ToList();

                if (игрыКатегории.Count == 0)
                    continue;

                ПанельСекцийКатегорий.Children.Add(
                    СоздатьСекциюКатегории(категория, игрыКатегории, избранные));
            }

            if (ПанельСекцийКатегорий.Children.Count == 0)
            {
                ПанельСекцийКатегорий.Children.Add(new TextBlock
                {
                    Text = "Игр не найдено.",
                    Foreground = (Brush)FindResource("ТекстВторичный"),
                    FontSize = 16,
                    Margin = new Thickness(0, 40, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
        }

        private UIElement СоздатьСекциюКатегории(string категория, List<Игра> игрыКатегории, HashSet<Guid> избранные)
        {
            var заголовок = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 0, 4, 10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Cursor = Cursors.Hand,
                Focusable = false
            };

            var строкаЗаголовка = new DockPanel();

            строкаЗаголовка.Children.Add(new TextBlock
            {
                Text = категория,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            var ссылкаВсе = new TextBlock
            {
                Text = $"Все ({игрыКатегории.Count})  →",
                Foreground = (Brush)FindResource("ТекстВторичный"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            DockPanel.SetDock(ссылкаВсе, Dock.Right);
            строкаЗаголовка.Children.Add(ссылкаВсе);

            заголовок.Content = строкаЗаголовка;

            заголовок.Click += (_, _) =>
            {
                выбраннаяКатегорияЦеликом = категория;
                ОбновитьСетку();
            };

            var миниКарусель = new КарусельИгр
            {
                Height = 330,
                Margin = new Thickness(0, 0, 0, 24)
            };

            миниКарусель.УстановитьМасштаб(0.62);
            миниКарусель.ОтключитьПрокруткуКолесом();

            миниКарусель.ИграЗапущена += ЗапускИгры;
            миниКарусель.ИзбранноеИзменилось += ПереключитьИзбранное;
            миниКарусель.ИграСкрыта += СкрытьИгру;

            миниКарусель.Загрузить(игрыКатегории, избранные);

            var секция = new StackPanel();
            секция.Children.Add(заголовок);
            секция.Children.Add(миниКарусель);

            return секция;
        }

        // =====================================================
        // РАЗВЁРНУТАЯ КАТЕГОРИЯ: ОБЫЧНАЯ СЕТКА, 5 В РЯД
        // =====================================================

        private void ПоказатьСеткуКатегории(string заголовок, List<Игра> игрыСписок, HashSet<Guid> избранные)
        {
            СкроллОбзораИгр.Visibility = Visibility.Collapsed;
            ПанельОднойКатегории.Visibility = Visibility.Visible;

            ЗаголовокОднойКатегории.Text = заголовок;

            СеткаОднойКатегории.Children.Clear();

            foreach (var игра in игрыСписок)
            {
                var карточка = new КарточкаИгрыКарусель
                {
                    Margin = new Thickness(0, 0, 18, 18),
                    // Карточка сама фиксированного размера (360×480) —
                    // LayoutTransform уменьшает её ЦЕЛИКОМ (и площадь,
                    // которую она занимает в layout'е), не трогая
                    // внутреннюю разметку/обрезку — безопаснее, чем
                    // менять Width/Height напрямую.
                    LayoutTransform = new ScaleTransform(0.7, 0.7)
                };

                карточка.Загрузить(игра, избранные.Contains(игра.Id));

                карточка.ИграЗапущена += ЗапускИгры;
                карточка.ИзбранноеИзменилось += ПереключитьИзбранное;
                карточка.ИграСкрыта += СкрытьИгру;

                СеткаОднойКатегории.Children.Add(карточка);
            }

            if (игрыСписок.Count == 0)
            {
                СеткаОднойКатегории.Children.Add(new TextBlock
                {
                    Text = "Ничего не найдено.",
                    Foreground = (Brush)FindResource("ТекстВторичный"),
                    FontSize = 16
                });
            }
        }

        private void НазадККатегориям_Click(object sender, RoutedEventArgs e)
        {
            выбраннаяКатегорияЦеликом = null;

            if (ПоискИгр.Text.Length > 0)
                ПоискИгр.Text = string.Empty;
            else
                ОбновитьСетку();
        }

        // =====================================================
        // МАГАЗИН
        // =====================================================

        private void ПоказатьКаталогМагазина_Click(object sender, RoutedEventArgs e)
        {
            СкроллКаталог.Visibility = Visibility.Visible;
            СкроллЗаказы.Visibility = Visibility.Collapsed;
            ВкладкаКаталог.Tag = "Активна";
            ВкладкаЗаказы.Tag = null;
        }

        private void ПоказатьЗаказыМагазина_Click(object sender, RoutedEventArgs e)
        {
            СкроллКаталог.Visibility = Visibility.Collapsed;
            СкроллЗаказы.Visibility = Visibility.Visible;
            ВкладкаЗаказы.Tag = "Активна";
            ВкладкаКаталог.Tag = null;

            ЗагрузитьЗаказы();
        }

        private async Task ЗагрузитьКаталогМагазина()
        {
            if (окноЗакрывается || каталогЗагружается) return;

            каталогЗагружается = true;

            try
            {
                var requestId = ShopCatalogBridgeService.CreateRequest();

                ShopCatalogDto? каталог = null;

                for (int i = 0; i < 20; i++)
                {
                    await Task.Delay(300);
                    if (окноЗакрывается) return;
                    каталог = ShopCatalogBridgeService.GetResult(requestId);
                    if (каталог != null) break;
                }

                if (каталог == null) return;

                ОтобразитьКаталог(каталог);
            }
            finally
            {
                каталогЗагружается = false;
            }
        }

        private void ОтобразитьКаталог(ShopCatalogDto каталог)
        {
            ПанельМагазина.Children.Clear();

            КнопкаМагазин.Visibility = каталог.Enabled ? Visibility.Visible : Visibility.Collapsed;

            ЛентаРекламы.Children.Clear();

            foreach (var item in каталог.Items.Take(8))
            {
                ЛентаРекламы.Children.Add(new TextBlock
                {
                    Text = $"   {item.Name} • {item.Price:0} ₽   ",
                    FontSize = 18,
                    Foreground = Brushes.White
                });
            }

            Анимации.ShopTickerAnimation.Start(ЛентаРекламы);

            if (!каталог.Enabled) return;

            foreach (var itemDto in каталог.Items)
            {
                var локальноеФото = ImageCacheService.SaveIfNeeded($"shop-{itemDto.Id}", itemDto.ImageData, itemDto.ImageExtension) ?? itemDto.Image;

                var item = new ShopItem
                {
                    Id = itemDto.Id,
                    CategoryId = itemDto.CategoryId,
                    Name = itemDto.Name,
                    Description = itemDto.Description,
                    Price = itemDto.Price,
                    Image = локальноеФото,
                    Featured = itemDto.Featured,
                    IsNew = itemDto.IsNew,
                    Stock = itemDto.Stock
                };

                var card = new КарточкаМагазина(item);
                card.BuyRequested += КупитьТовар;

                ПанельМагазина.Children.Add(card);
            }
        }

        private void КупитьТовар(ShopItem item)
        {
            if (аккаунт == null) return;

            var win = new ОкноВыбораПолучения { Owner = this };

            if (win.ShowDialog() != true) return;

            var requestId = ShopPurchaseBridgeService.CreateRequest(аккаунтId, компьютерId, item.Id, win.Result);

            for (int i = 0; i < 30; i++)
            {
                var результат = ShopPurchaseBridgeService.GetResult(requestId);

                if (результат.HasValue && результат.Value.Status != 0)
                {
                    if (результат.Value.Status == 1)
                    {
                        MessageBox.Show(
                            win.Result == ShopDeliveryType.BringToPc
                                ? "Администратор получил запрос и принесёт заказ."
                                : "Подойдите к администратору за заказом.",
                            "Заказ отправлен");
                    }
                    else
                    {
                        MessageBox.Show(результат.Value.Error ?? "Не удалось оформить заказ.", "Ошибка");
                    }

                    return;
                }

                System.Threading.Thread.Sleep(200);
            }

            MessageBox.Show("Сервер не ответил. Попробуйте ещё раз.", "Ошибка");
        }

        private async void ЗагрузитьЗаказы()
        {
            if (аккаунт == null) return;

            ПанельЗаказов.Children.Clear();
            ПанельЗаказов.Children.Add(new TextBlock { Text = "Загрузка...", Foreground = (Brush)FindResource("ТекстВторичный") });

            var requestId = ShopOrdersBridgeService.CreateRequest(аккаунтId);

            ShopOrdersDto? результат = null;

            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(200);
                if (окноЗакрывается) return;
                результат = ShopOrdersBridgeService.GetResult(requestId);
                if (результат != null) break;
            }

            ПанельЗаказов.Children.Clear();

            if (результат == null)
            {
                ПанельЗаказов.Children.Add(new TextBlock { Text = "Сервер не ответил. Нажмите вкладку ещё раз.", Foreground = (Brush)FindResource("Опасность") });
                return;
            }

            if (результат.Orders.Count == 0)
            {
                ПанельЗаказов.Children.Add(new TextBlock { Text = "У вас пока нет заказов.", Foreground = (Brush)FindResource("ТекстВторичный") });
                return;
            }

            foreach (var заказ in результат.Orders)
                ПанельЗаказов.Children.Add(СоздатьКарточкуЗаказа(заказ));
        }

        private UIElement СоздатьКарточкуЗаказа(ShopOrderDto заказ)
        {
            var (иконка, цвет, текстСтатуса) = ОтобразитьСтатус(заказ.Status);

            var header = new DockPanel();

            header.Children.Add(new TextBlock { Text = заказ.ItemName, Foreground = Brushes.White, FontSize = 17, FontWeight = FontWeights.Bold });

            var статусБлок = new TextBlock { Text = $"{иконка} {текстСтатуса}", Foreground = цвет, FontWeight = FontWeights.Bold };

            DockPanel.SetDock(статусБлок, Dock.Right);
            header.Children.Add(статусБлок);

            var подробности = new TextBlock
            {
                Text = $"{заказ.Price:0} ₽ • " +
                       (заказ.Delivery == "BringToPc" ? "Принести к ПК" : "Подойти к администратору") +
                       $" • {заказ.Time:dd.MM HH:mm}",
                Foreground = Brushes.LightGray,
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var stack = new StackPanel();
            stack.Children.Add(header);
            stack.Children.Add(подробности);

            return new Border { Style = (Style)FindResource("КарточкаМалая"), Margin = new Thickness(0, 0, 0, 12), Child = stack };
        }

        private static (string Icon, Brush Color, string Text) ОтобразитьСтатус(string status) => status switch
        {
            "Pending" => ("⏳", Brushes.Gold, "Ожидает"),
            "Preparing" => ("🛠", Brushes.DodgerBlue, "Готовится"),
            "Ready" => ("✅", Brushes.LimeGreen, "Готово"),
            "Completed" => ("📦", Brushes.Gray, "Выдано"),
            "Cancelled" => ("✕", Brushes.OrangeRed, "Отменено"),
            _ => ("•", Brushes.Gray, status)
        };

        // =====================================================
        // ФОРМАТ ВРЕМЕНИ
        // =====================================================

        private static string ФорматСекунды(TimeSpan время)
        {
            if (время < TimeSpan.Zero) время = TimeSpan.Zero;
            if (время.TotalHours >= 100) return $"{(int)время.TotalHours}:{время.Minutes:00}:{время.Seconds:00}";
            return время.ToString(@"hh\:mm\:ss");
        }

        private static string ФорматКороткий(TimeSpan время)
        {
            if (время < TimeSpan.Zero) время = TimeSpan.Zero;
            if (время.TotalHours >= 100) return $"{(int)время.TotalHours}:{время.Minutes:00}";
            return время.ToString(@"hh\:mm");
        }
    }
}