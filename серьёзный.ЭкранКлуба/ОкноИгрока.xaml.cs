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


namespace серьёзный.ЭкранКлуба
{
    public partial class ОкноИгрока : Window
    {
        private readonly DispatcherTimer таймер = new();
        private readonly DispatcherTimer обновление = new();

        private readonly GameSessionTracker трекер = new();

        private readonly СервисАккаунтов сервисАккаунтов = new();

        private readonly СервисИгр сервисИгр = new();

        private readonly СервисНастроекИгрока сервисНастроек = new();

        private readonly DirectMessageService direct =
    new();

        private НастройкиИгрока настройкиИгрока = new();

        private readonly ShopService магазин =
    new();

        private readonly SocialService social =
    new();

        private List<Игра> игры = new();

        private readonly Guid аккаунтId;
        private readonly int компьютерId;

        private АккаунтИгрока? аккаунт;

        private Окна.ОкноЧата? окноЧата;

        private bool окноЗакрывается;

        public ОкноИгрока(Guid idАккаунта, int idПК)
        {
            InitializeComponent();

            КнопкаРазвлечения.Click += (_, _) =>
            {
                new ОкноРазвлеченияИгрока(аккаунтId) { Owner = this }.ShowDialog();
            };

            ShopChangedEvent.Changed += МагазинИзменился;

            Loaded += (_, _) =>
            {
                ПостроитьМагазин();
            };

            Closed += (_, _) =>
            {
                ShopChangedEvent.Changed -= МагазинИзменился;
            };

            аккаунтId = idАккаунта;
            компьютерId = idПК;

            Loaded += ПриЗагрузке;
            Closed += ПриЗакрытии;

            MouseLeftButtonDown += ПеретаскиваниеОкна;

            КнопкаГлавная.Click += (_, _) => ПоказатьГлавную();
            КнопкаИгры.Click += (_, _) => ПоказатьИгры();
            КнопкаМагазин.Click += (_, _) => ПоказатьМагазин();

            КнопкаИгроки.Click += (_, _) =>
            {
                new Окна.ОкноИгроки(
                    аккаунтId).ShowDialog();
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

        // =====================================================
        // ЗАГРУЗКА
        // =====================================================

        private void ПриЗагрузке(object? sender, RoutedEventArgs e)
        {
            аккаунт = сервисАккаунтов.ПеречитатьИзБазы(аккаунтId);

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

            ПостроитьИгры();

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
                ПостроитьИгры();
            });
        }

        private void ПриЗакрытии(object? sender, EventArgs e)
        {
            окноЗакрывается = true;

            таймер.Stop();
            обновление.Stop();

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

            ПостроитьИгры();
            ОбновитьИнформацию();

            if (аккаунт == null)
                return;

            if (аккаунт.ОсталосьВремени <= TimeSpan.Zero)
            {
                ЗакрытьОкно();
            }
        }

        private void ОбновлениеАккаунта(
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
                сервисАккаунтов.Получить(аккаунтId);

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

        private void ПостроитьИгры()
        {
            игры =
                сервисИгр
                    .ПолучитьИгры(компьютерId)
                    .Where(x => !x.Скрыта)
                    .ToList();

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
                if (аккаунт == null)
                    return;

                аккаунт.ВсегоСыграно += время;
                аккаунт.ВсегоСеансов++;

                сервисАккаунтов.ОбновитьВоВремяСеанса(
    аккаунт.Id,
    время,
    false);

                сервисАккаунтов.ЗавершитьСтатистику(
                    аккаунт.Id);

                ОбновитьИнформацию();
            });
        }

        private void ПереключитьИзбранное(Игра игра)
        {
            if (настройкиИгрока.Избранное.Contains(игра.Id))
                настройкиИгрока.Избранное.Remove(игра.Id);
            else
                настройкиИгрока.Избранное.Add(игра.Id);

            сервисНастроек.Сохранить(настройкиИгрока);

            ПостроитьИгры();
        }

        private void ПоискИгр_TextChanged(
    object sender,
    TextChangedEventArgs e)
        {
            ПостроитьИгры();
        }

        private void Категории_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            ПостроитьИгры();
        }

        // =====================================================
        // ЧАТ
        // =====================================================

        private void ОткрытьЧат()
        {
            if (аккаунт == null)
                return;

            if (окноЧата == null)
            {
                окноЧата = new Окна.ОкноЧата
                {
                    МойId = аккаунт.Id
                };

                окноЧата.Closed += (_, _) =>
                {
                    окноЧата = null;
                };
            }

            окноЧата.ИмяАдминистратора = "Администратор";

            окноЧата.УстановитьЛичныйЧат(
                Guid.Empty,
                "Администратор");

            окноЧата.Show();
            окноЧата.Activate();
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

        private void МагазинИзменился()
        {
            Dispatcher.Invoke(ПостроитьМагазин);
        }

        private void ПостроитьМагазин()
        {
            ПанельМагазина.Children.Clear();

            var settings =
                магазин.GetSettings();

            КнопкаМагазин.Visibility =
                settings.Enabled
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ЛентаРекламы.Children.Clear();

            foreach (var item in магазин.GetItems().Take(8))
            {
                ЛентаРекламы.Children.Add(
                    new TextBlock
                    {
                        Text = $"   {item.Name} • {item.Price:0} ₽   ",
                        FontSize = 20,
                        Foreground =
                            Brushes.White
                    });
            }

            Анимации.ShopTickerAnimation.Start(
                ЛентаРекламы);

            if (!settings.Enabled)
                return;

            foreach (var item in магазин.GetItems())
            {
                if (item.Hidden)
                    continue;

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

            var requests = new ShopRequestService();

            requests.Create(
                аккаунтId,
                компьютерId,
                item.Id,
                item.Name,
                item.Price,
                win.Result);

            MessageBox.Show(
                win.Result == ShopDeliveryType.BringToPc
                    ? "Администратор получил запрос и принесёт заказ."
                    : "Подойдите к администратору за заказом.",
                "Заказ отправлен");
        }
    }


}