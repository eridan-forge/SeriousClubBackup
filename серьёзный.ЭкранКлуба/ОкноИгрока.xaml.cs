using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using серьёзный.Core.CoreChat;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreLaunch;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreSocial;
using серьёзный.CrystalUI.Steam;
using серьёзный.Модели;
using серьёзный.Окна;
using серьёзный.Сервисы;
using серьёзный.ЭкранКлуба.Модели;
using серьёзный.ЭкранКлуба.Сервисы;
using серьёзный.Карточки;
using серьёзный.Core.CoreServices;
using серьёзный.Core.CoreModels;
using System.Threading;

namespace серьёзный.ЭкранКлуба
{
    public partial class ОкноИгрока : Window
    {
        private readonly DispatcherTimer таймер = new();
        private readonly DispatcherTimer обновление = new();

        private readonly GameSessionTracker трекер = new();

       

        private readonly СервисИгр сервисИгр = new();

        private readonly СервисНастроекИгрока сервисНастроек = new();

        private readonly DispatcherTimer игрыОбновление = new()
        {
            Interval = TimeSpan.FromSeconds(15)
        };

        private bool каталогИгрЗагружается;

        private readonly DirectMessageService direct =
    new();

        private НастройкиИгрока настройкиИгрока = new();

        private readonly DispatcherTimer магазинОбновление = new()
        {
            Interval = TimeSpan.FromSeconds(5)
        };

        private bool каталогЗагружается;

        private readonly SocialService social =
    new();

        private List<Игра> игры = new();

        private readonly Guid аккаунтId;
        private readonly int компьютерId;

        private АккаунтИгрока? аккаунт;

        private ОкноЛичногоЧата? окноЛичногоЧата;

        private bool окноЗакрывается;


        public ОкноИгрока(Guid idАккаунта, int idПК)
        {
            InitializeComponent();

            КнопкаРазвлечения.Click += (_, _) =>
            {
                new ОкноРазвлеченияИгрока(аккаунтId) { Owner = this }.ShowDialog();
            };

            Loaded += (_, _) =>
            {
                ЗагрузитьКаталогМагазина();

                магазинОбновление.Tick += (_, _) => ЗагрузитьКаталогМагазина();
                магазинОбновление.Start();
            };

            Closed += (_, _) =>
            {
                магазинОбновление.Stop();
            };

            аккаунтId = idАккаунта;
            компьютерId = idПК;

            Loaded += ПриЗагрузке;
            Closed += ПриЗакрытии;

            MouseLeftButtonDown += ПеретаскиваниеОкна;

            КнопкаГлавная.Click += (_, _) => ПоказатьГлавную();
            КнопкаИгры.Click += (_, _) => ПоказатьИгры();
            КнопкаМагазин.Click += (_, _) => ПоказатьМагазин();
            КнопкаЗаказы.Click += (_, _) => ПоказатьЗаказы();
            КнопкаОбновитьЗаказы.Click += (_, _) => ЗагрузитьЗаказы();

            КнопкаИгроки.Click += (_, _) =>
            {
                new Окна.ОкноИгроки(
                    аккаунтId,
                     аккаунт?.ПолноеИмя ?? "Игрок").ShowDialog();
            };

            КнопкаЧат.Click += (_, _) => ОткрытьЧат();

            КнопкаМинимизировать.Click += (_, _) =>
                WindowState = WindowState.Minimized;

            КнопкаРазвернуть.Click += (_, _) =>
            {
                WindowState =
                    WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
            };

            КнопкаЗакрыть.Click += (_, _) =>
    WindowState = WindowState.Minimized;

            КнопкаСвернуть.Click += (_, _) =>
                WindowState = WindowState.Minimized;


        }
        private async Task<АккаунтИгрока?> ЗапроситьАккаунтЧерезСерверAsync()
        {
            var requestId = AccountBalanceBridgeService.CreateRequest(аккаунтId);

            BalanceRequestRecord? результат = null;

            for (int i = 0; i < 30; i++) // до ~6 секунд
            {
                await Task.Delay(200);

                результат = AccountBalanceBridgeService.GetResult(requestId);

                if (результат != null && результат.Done)
                    break;
            }

            if (результат == null || !результат.Done || результат.Failed)
            {
                // Сервер недоступен прямо сейчас — не убиваем окно резко,
                // отдаём последнее известное локальное состояние, если есть.
                return аккаунт;
            }

            var базовый = аккаунт ?? new АккаунтИгрока { Id = аккаунтId };

            базовый.ОсталосьВремени = TimeSpan.FromSeconds(результат.RemainingSeconds);
            базовый.ВсегоСыграно = TimeSpan.FromSeconds(результат.PlayedSeconds);
            базовый.ВсегоСеансов = результат.SessionCount;

            return базовый;
        }

        // =====================================================
        // ЗАГРУЗКА
        // =====================================================

        private async void ПриЗагрузке(object? sender, RoutedEventArgs e)
        {
            аккаунт = await ЗапроситьАккаунтЧерезСерверAsync();


            if (аккаунт == null)
            {
                Close();
                return;
            }

            social.SetOnline(
    аккаунт.Id,
    компьютерId,
    null);

            ИмяИгрока.Text = аккаунт.ПолноеИмя;
            ТекстПК.Text = $"ПК-{компьютерId}";

            настройкиИгрока =
сервисНастроек.Загрузить(аккаунт.Id);

            ЗагрузитьКаталогИгр();

            игрыОбновление.Tick += (_, _) => ЗагрузитьКаталогИгр();
            игрыОбновление.Start();

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

            Dispatcher.Invoke(() =>
            {
                ЗагрузитьКаталогИгр();
            });
        }

        private void ПриЗакрытии(object? sender, EventArgs e)
        {
            окноЗакрывается = true;

            таймер.Stop();
            обновление.Stop();
            игрыОбновление.Stop();

            LiveGameSync.Refresh -= ОбновитьКарточки;

            if (аккаунт != null)
            {
                social.SetOffline(аккаунт.Id);
            }
        }


        // =====================================================
        // ПЕРЕТАСКИВАНИЕ
        // =====================================================

        private void ПеретаскиваниеОкна(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch
                {
                }
            }
        }

        // =====================================================
        // ОБНОВЛЕНИЕ
        // =====================================================

        private void Таймер(
            object? sender,
            EventArgs e)
        {
            if (окноЗакрывается)
                return;

            ОбновитьСетку();
            ОбновитьИнформацию();

            ПроверитьДостижения();

            if (аккаунт == null)
                return;

            if (аккаунт.ОсталосьВремени <= TimeSpan.Zero)
            {
                ЗакрытьОкно();
            }
        }

        private async void ОбновлениеАккаунта(
             object? sender,
            EventArgs e)
        {
            if (окноЗакрывается)
                return;

            if (НужноЗакрытьОкно())
            {
                ЗакрытьОкно();
                return;
            }

            аккаунт =
                 await ЗапроситьАккаунтЧерезСерверAsync();

            if (аккаунт == null)
            {
                ЗакрытьОкно();
                return;
            }

            ОбновитьИнформацию();
        }

        // -----------------------------------------------------
        // Сеанс на этом ПК мог быть завершён администратором
        // (или переназначен на другой аккаунт) уже после того,
        // как открылось это окно. Проверяем состояние экрана
        // клуба, которое обновляет Патруль при завершении сеанса.
        // -----------------------------------------------------

        private bool НужноЗакрытьОкно()
        {
            try
            {
                var состояние =
                    StateService.Загрузить();

                if (состояние.Locked)
                    return true;

                if (!состояние.AccountId.HasValue)
                    return true;

                return состояние.AccountId.Value != аккаунтId;
            }
            catch
            {
                // Временная ошибка чтения БД — не закрываем
                // окно игрока из-за неё.
                return false;
            }
        }

        // -----------------------------------------------------
        // Закрывает окно игрока так, чтобы экран клуба (который
        // отдельно следит за Locked через свой собственный таймер)
        // корректно вышел на передний план, а не остался позади
        // уже открытого диалога окна игрока.
        // -----------------------------------------------------

        private void ЗакрытьОкно()
        {
            if (окноЗакрывается)
                return;

            окноЗакрывается = true;

            таймер.Stop();
            обновление.Stop();

            WindowState = WindowState.Minimized;

            Close();
        }

        private void ОбновитьИнформацию()
        {
            if (аккаунт == null)
                return;

            ИмяИгрока.Text =
                аккаунт.ПолноеИмя;

            var время =
                аккаунт.ОсталосьВремени;

            if (время < TimeSpan.Zero)
                время = TimeSpan.Zero;

            БольшойТаймер.Text =
                ФорматСекунды(время);

            ТекстСыграно.Text =
                ФорматКороткий(аккаунт.ВсегоСыграно);

            ТекстСеансов.Text =
                аккаунт.ВсегоСеансов.ToString();

            if (время.TotalMinutes <= 5)
            {
                ТекстСтатус.Text =
                    "Мало времени";

                ТекстСтатус.Foreground =
                    Brushes.OrangeRed;
            }
            else
            {
                ТекстСтатус.Text =
                    "Играет";

                ТекстСтатус.Foreground =
                    Brushes.LimeGreen;
            }
        }


        private void ПроверитьДостижения()
        {
            if (аккаунт == null)
                return;

            try
            {
                var уведомление =
                    AchievementNotificationBridgeService.TakeNextPending(аккаунтId);

                if (уведомление == null)
                    return;

                AchievementNotificationBridgeService.MarkDelivered(уведомление.Id);

                new серьёзный.ЭкранКлуба.Уведомления.AchievementToast(
                    уведомление.Name,
                    уведомление.Description).Show();
            }
            catch
            {
                // Тост о достижении не должен ронять окно игрока.
            }
        }

        // =====================================================
        // ВКЛАДКИ
        // =====================================================

        private void ПоказатьГлавную()
        {
            СтраницаГлавная.Visibility =
                Visibility.Visible;

            СтраницаИгры.Visibility =
                Visibility.Collapsed;

            СтраницаМагазин.Visibility =
                Visibility.Collapsed;

            Выделить(КнопкаГлавная);
        }

        private void ПоказатьИгры()
        {
            СтраницаГлавная.Visibility =
                Visibility.Collapsed;

            СтраницаИгры.Visibility =
                Visibility.Visible;

            СтраницаМагазин.Visibility =
                Visibility.Collapsed;

            Выделить(КнопкаИгры);
        }

        private void ПоказатьМагазин()
        {
            СтраницаГлавная.Visibility =
                Visibility.Collapsed;

            СтраницаИгры.Visibility =
                Visibility.Collapsed;

            СтраницаМагазин.Visibility =
                Visibility.Visible;

            Выделить(КнопкаМагазин);
        }

        private void ПоказатьЗаказы()
        {
            СтраницаГлавная.Visibility =
                Visibility.Collapsed;

            СтраницаИгры.Visibility =
                Visibility.Collapsed;

            СтраницаМагазин.Visibility =
                Visibility.Collapsed;

            СтраницаЗаказы.Visibility =
                Visibility.Visible;

            Выделить(КнопкаЗаказы);

            ЗагрузитьЗаказы();
        }

        private async void ЗагрузитьКаталогИгр()
        {
            if (окноЗакрывается || каталогИгрЗагружается)
                return;

            каталогИгрЗагружается = true;

            try
            {
                var requestId = GameCatalogBridgeService.CreateRequest(компьютерId);

                GameCatalogDto? каталог = null;

                for (int i = 0; i < 20; i++) // до ~6 секунд
                {
                    await Task.Delay(300);

                    if (окноЗакрывается)
                        return;

                    каталог = GameCatalogBridgeService.GetResult(requestId);

                    if (каталог != null)
                        break;
                }

                if (каталог == null)
                    return; // сервер не ответил — оставляем то, что уже показано

                игры = каталог.Games
                    .Select(x => new Игра
                    {
                        Id = x.Id,
                        Название = x.Название,
                        Категория = x.Категория,
                        Описание = x.Описание,
                        Путь = x.Путь,
                        Обложка = x.Обложка,
                        Порядок = x.Порядок
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
            var выбраннаяКатегория =
                Категории.SelectedItem as string ?? "Все";

            СеткаИгр.Children.Clear();

            var поиск =
                ПоискИгр.Text.Trim().ToLower();

            foreach (var игра in игры)
            {
                if (!string.IsNullOrWhiteSpace(поиск))
                {
                    if (!игра.Название.ToLower().Contains(поиск))
                        continue;
                }

                if (выбраннаяКатегория != "Все")
                {
                    if (игра.Категория != выбраннаяКатегория)
                        continue;
                }

                СеткаИгр.Children.Add(
                    СоздатьКарточку(игра));
            }

            var списокКатегорий =
                new[] { "Все" }
                    .Concat(
                        игры.Select(x => x.Категория)
                            .Distinct())
                    .ToList();

            Категории.ItemsSource =
                списокКатегорий;

            if (!списокКатегорий.Contains(выбраннаяКатегория))
                выбраннаяКатегория = "Все";

            Категории.SelectedItem =
                выбраннаяКатегория;
        }

        private Border СоздатьКарточку(Игра игра)
        {
            var картинка =
                new Image
                {
                    Height = 150,
                    Stretch = Stretch.UniformToFill
                };

            if (File.Exists(игра.Обложка))
            {
                картинка.Source =
                    new BitmapImage(
                        new Uri(игра.Обложка));
            }

            var избранное =
                new Button
                {
                    Content =
                        настройкиИгрока.Избранное.Contains(игра.Id)
                            ? "★"
                            : "☆",
                    Width = 34,
                    Height = 34,
                    Margin = new Thickness(6),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

            избранное.Click += (_, _) =>
            {
                ПереключитьИзбранное(игра);
            };

            var играть =
                new Button
                {
                    Content = "Играть",
                    Margin = new Thickness(10),
                    Height = 36,
                    Background =
                        Brushes.DodgerBlue,
                    Foreground =
                        Brushes.White
                };

            играть.Click += (_, _) =>
            {
                ЗапускИгры(игра);
            };

            return new Border
            {
                Width = 210,
                Margin = new Thickness(12),
                CornerRadius = new CornerRadius(18),
                Background =
                    new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Child =
                    new StackPanel
                    {
                        Children =
                        {
                    картинка,
                    избранное,
                    new TextBlock
                    {
                        Text = игра.Название,
                        Margin = new Thickness(10,6,10,0),
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold,
                        TextAlignment = TextAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = игра.Категория,
                        Foreground = Brushes.Gray,
                        TextAlignment = TextAlignment.Center
                    },
                    играть
                        }
                    }
            };
        }

        private void ЗапускИгры(Игра игра)
        {
            var model =
                new серьёзный.Core.CoreModels.GameInfo
                {
                    Name = игра.Название,
                    Path = игра.Путь,

                    // Пока безопасные значения
                    Launcher = "",
                    AppId = "",
                    LaunchArguments = ""
                };

            var process =
                GameLaunchService.Launch(model);

            трекер.Start(process);

            трекер.Finished -= ИграЗакрылась;
            трекер.Finished += ИграЗакрылась;
        }

        private void ИграЗакрылась(TimeSpan время)
        {
            Dispatcher.Invoke(() =>
            {
                if (аккаунт == null || время <= TimeSpan.Zero)
                    return;

                // Клиент только сообщает факт - баланс и статистику
                // применяет сервер. Экран сам подтянет актуальные
                // цифры на следующем тике ОбновлениеАккаунта (раз
                // в 2 секунды, уже работает через AccountBalanceBridge).
                GameSessionReportBridgeService.CreateRequest(
аккаунт.Id,
  компьютерId,
   (long)время.TotalSeconds);

            });
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

        private void ПоискИгр_TextChanged(
object sender,
TextChangedEventArgs e)
        {
            ОбновитьСетку();
        }

        private void Категории_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ОбновитьСетку();
        }

        // =====================================================
        // ЧАТ
        // =====================================================

        private void ОткрытьЧат()
        {
            if (аккаунт == null)
                return;

            if (окноЛичногоЧата == null)
            {
                окноЛичногоЧата = new ОкноЛичногоЧата(
  компьютерId,
   аккаунт.ПолноеИмя);

                окноЛичногоЧата.Closed += (_, _) =>
                {
                окноЛичногоЧата = null;
                };
            }

            окноЛичногоЧата.Show();
            окноЛичногоЧата.Activate();
        }



        // =====================================================
        // ПОДСВЕТКА КНОПОК
        // =====================================================

        private void Выделить(Button активная)
        {
            СброситьКнопки();

            активная.Background =
                new SolidColorBrush(
                    Color.FromRgb(37, 99, 235));

            активная.Foreground =
                Brushes.White;
        }

        private void СброситьКнопки()
        {
            var фон =
                new SolidColorBrush(
                    Color.FromRgb(30, 41, 59));

            foreach (var кнопка in new[]
            {
                КнопкаГлавная,
                КнопкаИгры,
                КнопкаМагазин,
                КнопкаЗаказы,
                КнопкаЧат
            })
            {
                кнопка.Background = фон;
                кнопка.Foreground = Brushes.White;
            }
        }

        // =====================================================
        // ФОРМАТЫ ВРЕМЕНИ
        // =====================================================

        private static string ФорматСекунды(TimeSpan время)
        {
            if (время < TimeSpan.Zero)
                время = TimeSpan.Zero;

            if (время.TotalHours >= 100)
            {
                return $"{(int)время.TotalHours}:{время.Minutes:00}:{время.Seconds:00}";
            }

            return время.ToString(@"hh\:mm\:ss");
        }

        private static string ФорматКороткий(TimeSpan время)
        {
            if (время < TimeSpan.Zero)
                время = TimeSpan.Zero;

            if (время.TotalHours >= 100)
            {
                return $"{(int)время.TotalHours}:{время.Minutes:00}";
            }

            return время.ToString(@"hh\:mm");
        }

        private async void ЗагрузитьКаталогМагазина()
        {
            if (окноЗакрывается || каталогЗагружается)
                return;

            каталогЗагружается = true;

            try
            {
                var requestId = ShopCatalogBridgeService.CreateRequest();

                ShopCatalogDto? каталог = null;

                for (int i = 0; i < 20; i++) // до ~6 секунд
                {
                    await Task.Delay(300);

                    if (окноЗакрывается)
                        return;

                    каталог = ShopCatalogBridgeService.GetResult(requestId);

                    if (каталог != null)
                        break;
                }

                if (каталог == null)
                    return; // сервер не ответил — оставляем то, что уже показано

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

            КнопкаМагазин.Visibility =
                каталог.Enabled
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ЛентаРекламы.Children.Clear();

            foreach (var item in каталог.Items.Take(8))
            {
                ЛентаРекламы.Children.Add(
                    new TextBlock
                    {
                        Text = $"   {item.Name} • {item.Price:0} ₽   ",
                        FontSize = 20,
                        Foreground = Brushes.White
                    });
            }

            Анимации.ShopTickerAnimation.Start(ЛентаРекламы);

            if (!каталог.Enabled)
                return;

            foreach (var itemDto in каталог.Items)
            {
                var item = new ShopItem
                {
                    Id = itemDto.Id,
                    CategoryId = itemDto.CategoryId,
                    Name = itemDto.Name,
                    Description = itemDto.Description,
                    Price = itemDto.Price,
                    Image = itemDto.Image,
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
            if (аккаунт == null)
                return;

            var win = new ОкноВыбораПолучения
            {
                Owner = this
            };

            if (win.ShowDialog() != true)
                return;

            var requestId =
                серьёзный.Core.CoreServices.ShopPurchaseBridgeService.CreateRequest(
                     аккаунтId, компьютерId, item.Id, win.Result);

            // Заказ реально существует только на сервере (у админа);
            // ждём подтверждения, как и при логине/балансе.
            for (int i = 0; i < 30; i++) // до ~6 секунд
            {
                var результат =
    серьёзный.Core.CoreServices.ShopPurchaseBridgeService.GetResult(requestId);

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
                        MessageBox.Show(
  результат.Value.Error ?? "Не удалось оформить заказ.",
    "Ошибка");
                    }


                                   return;
                }

                System.Threading.Thread.Sleep(200);
            }

            MessageBox.Show("Сервер не ответил. Попробуйте ещё раз.", "Ошибка");

        }


        private async void ЗагрузитьЗаказы()
        {
            if (аккаунт == null)
                return;

            ПанельЗаказов.Children.Clear();

            ПанельЗаказов.Children.Add(
                new TextBlock
                {
                    Text = "Загрузка...",
                    Foreground = Brushes.Gray,
                    FontSize = 15
                });

            var requestId =
                ShopOrdersBridgeService.CreateRequest(аккаунтId);

            ShopOrdersDto? результат = null;

            for (int i = 0; i < 30; i++) // до ~6 секунд
            {
                await Task.Delay(200);

                if (окноЗакрывается)
                    return;

                результат = ShopOrdersBridgeService.GetResult(requestId);

                if (результат != null)
                    break;
            }

            ПанельЗаказов.Children.Clear();

            if (результат == null)
            {
                ПанельЗаказов.Children.Add(
                    new TextBlock
                    {
                        Text = "Сервер не ответил. Нажмите «Обновить».",
                        Foreground = Brushes.OrangeRed,
                        FontSize = 15
                    });

                return;
            }

            if (результат.Orders.Count == 0)
            {
                ПанельЗаказов.Children.Add(
                    new TextBlock
                    {
                        Text = "У вас пока нет заказов.",
                        Foreground = Brushes.Gray,
                        FontSize = 15
                    });

                return;
            }

            foreach (var заказ in результат.Orders)
            {
                ПанельЗаказов.Children.Add(СоздатьКарточкуЗаказа(заказ));
            }
        }

        private static UIElement СоздатьКарточкуЗаказа(ShopOrderDto заказ)
        {
            var (иконка, цвет, текстСтатуса) = ОтобразитьСтатус(заказ.Status);

            var header = new DockPanel();

            header.Children.Add(
                new TextBlock
                {
                    Text = заказ.ItemName,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                });

            var статусБлок =
                new TextBlock
                {
                    Text = $"{иконка} {текстСтатуса}",
                    Foreground = цвет,
                    FontWeight = FontWeights.Bold
                };

            DockPanel.SetDock(статусБлок, Dock.Right);
            header.Children.Add(статусБлок);

            var подробности =
                new TextBlock
                {
                    Text =
                        $"{заказ.Price:0} ₽ • " +
                        (заказ.Delivery == "BringToPc"
                            ? "Принести к ПК"
                            : "Подойти к администратору") +
                        $" • {заказ.Time:dd.MM HH:mm}",
                    Foreground = Brushes.LightGray,
                    FontSize = 13,
                    Margin = new Thickness(0, 6, 0, 0)
                };

            var stack = new StackPanel();

            stack.Children.Add(header);
            stack.Children.Add(подробности);

            return new Border
            {
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(14),
                Background =
                    new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Child = stack
            };
        }

        private static (string Icon, Brush Color, string Text) ОтобразитьСтатус(string status)
        {
            return status switch
            {
                "Pending" => ("⏳", Brushes.Gold, "Ожидает"),
                "Preparing" => ("🛠", Brushes.DodgerBlue, "Готовится"),
                "Ready" => ("✅", Brushes.LimeGreen, "Готово"),
                "Completed" => ("📦", Brushes.Gray, "Выдано"),
                "Cancelled" => ("✕", Brushes.OrangeRed, "Отменено"),
                _ => ("•", Brushes.Gray, status)
            };
        }
    }




}