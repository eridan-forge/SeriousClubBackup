using System;
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
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;
using серьёзный.Модели;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноПодробностейИгры : Window
{
    private readonly Guid аккаунтId;
    private readonly Игра игра;

    private DispatcherTimer? таймерТекста;
    private int индексТекста;
    private GameCommunityDetailsDto? данные;

    public event Action<Игра>? Играть;

    public ОкноПодробностейИгры(Guid аккаунтId, string имяИгрока, Игра игра)
    {
        InitializeComponent();

        this.аккаунтId = аккаунтId;
        this.игра = игра;

        Название.Text = игра.Название;
        Категория.Text = игра.Категория;

        if (!string.IsNullOrWhiteSpace(игра.Обложка) && File.Exists(игра.Обложка))
        {
            var картинка = new BitmapImage();
            картинка.BeginInit();
            картинка.CacheOption = BitmapCacheOption.OnLoad;
            картинка.UriSource = new Uri(игра.Обложка);
            картинка.EndInit();
            картинка.Freeze();

            Обложка.Source = картинка;
            Заглушка.Visibility = Visibility.Collapsed;
        }
        else
        {
            Заглушка.Visibility = Visibility.Visible;
        }

        Loaded += ПриЗагрузке_Анимация;
        Loaded += (_, _) => _ = ЗагрузитьAsync();
        Closed += (_, _) => таймерТекста?.Stop();
    }

    private void ПриЗагрузке_Анимация(object sender, RoutedEventArgs e)
    {
        var область = SystemParameters.WorkArea;

        Top = область.Top + 30;

        var финишныйLeft = область.Left + 30;

        Left = область.Left - Width;

        var анимация = new DoubleAnimation(Left, финишныйLeft, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(LeftProperty, анимация);
    }

    private void Закрыть_Click(object sender, RoutedEventArgs e)
    {
        var область = SystemParameters.WorkArea;

        var анимация = new DoubleAnimation(Left, область.Left - Width, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        анимация.Completed += (_, _) => Close();

        BeginAnimation(LeftProperty, анимация);
    }

    private void Играть_Click(object sender, RoutedEventArgs e) => Играть?.Invoke(игра);

    // =====================================================
    // ВКЛАДКИ
    // =====================================================

    private void ПоказатьОписание_Click(object sender, RoutedEventArgs e) => Переключить(СтраницаОписание, ВкладкаОписание);
    private void ПоказатьОтзывы_Click(object sender, RoutedEventArgs e) => Переключить(СтраницаОтзывы, ВкладкаОтзывы);
    private void ПоказатьПомощь_Click(object sender, RoutedEventArgs e) => Переключить(СтраницаПомощь, ВкладкаПомощь);

    private void Переключить(UIElement страница, Button вкладка)
    {
        СтраницаОписание.Visibility = Visibility.Collapsed;
        СтраницаОтзывы.Visibility = Visibility.Collapsed;
        СтраницаПомощь.Visibility = Visibility.Collapsed;

        ВкладкаОписание.Tag = null;
        ВкладкаОтзывы.Tag = null;
        ВкладкаПомощь.Tag = null;

        страница.Visibility = Visibility.Visible;
        вкладка.Tag = "Активна";
    }

    // =====================================================
    // ДАННЫЕ СООБЩЕСТВА
    // =====================================================

    private async Task ЗагрузитьAsync()
    {
        var результат = await ЗапроситьAsync(new GameCommunityRequestDto
        {
            Action = GameCommunityAction.GetDetails,
            GameName = игра.Название
        });

        if (результат?.Details == null)
            return;

        данные = результат.Details;

        Отрисовать();
    }

    private async Task<GameCommunityResultDto?> ЗапроситьAsync(GameCommunityRequestDto запрос)
    {
        var requestId = GameCommunityBridgeService.CreateRequest(аккаунтId, запрос);

        for (int i = 0; i < 25; i++)
        {
            await Task.Delay(200);

            var результат = GameCommunityBridgeService.GetResult(requestId);

            if (результат != null)
                return результат;
        }

        return null;
    }

    private void Отрисовать()
    {
        if (данные == null)
            return;

        ПостроитьЗвёзды(ПанельЗвёзд, (int)Math.Round(данные.AverageRating), звезда => _ = ОценитьAsync(звезда));

        ТекстРейтинг.Text = данные.RatingCount == 0
            ? "Оценок пока нет — стань первым!"
            : $"{данные.AverageRating:0.0} из 5 • {данные.RatingCount} оценок";

        ВкладкаОтзывы.Content = $"Отзывы ({данные.ReviewCount})";

        ЗапуститьАнимациюТекста(
            ТекстОписание,
            string.IsNullOrWhiteSpace(игра.Описание)
                ? "Администратор ещё не добавил описание этой игры."
                : игра.Описание);

        ОтрисоватьОтзывы();
        ОтрисоватьПомощь();
    }

    private async Task ОценитьAsync(int звезда)
    {
        await ЗапроситьAsync(new GameCommunityRequestDto
        {
            Action = GameCommunityAction.RateGame,
            GameName = игра.Название,
            Stars = звезда
        });

        await ЗагрузитьAsync();
    }

    private void ОтрисоватьОтзывы()
    {
        СписокОтзывов.Children.Clear();

        if (данные!.Reviews.Count == 0)
        {
            СписокОтзывов.Children.Add(new TextBlock
            {
                Text = "Отзывов пока нет.",
                Foreground = (Brush)FindResource("ТекстВторичный")
            });

            return;
        }

        foreach (var отзыв in данные.Reviews)
        {
            var стек = new StackPanel();
            var верх = new DockPanel();

            верх.Children.Add(new TextBlock
            {
                Text = отзыв.PlayerName,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            });

            var звёзды = new TextBlock
            {
                Text = new string('★', Math.Clamp(отзыв.Stars, 0, 5)) +
                       new string('☆', 5 - Math.Clamp(отзыв.Stars, 0, 5)),
                Foreground = (Brush)FindResource("Золото")
            };

            DockPanel.SetDock(звёзды, Dock.Right);
            верх.Children.Add(звёзды);

            стек.Children.Add(верх);

            стек.Children.Add(new TextBlock
            {
                Text = отзыв.Text,
                Foreground = (Brush)FindResource("ТекстВторичный"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            стек.Children.Add(new TextBlock
            {
                Text = отзыв.Time.ToString("dd.MM.yyyy HH:mm"),
                Foreground = (Brush)FindResource("ТекстПриглушённый"),
                FontSize = 11,
                Margin = new Thickness(0, 6, 0, 0)
            });

            СписокОтзывов.Children.Add(new Border
            {
                Style = (Style)FindResource("КарточкаМалая"),
                Margin = new Thickness(0, 0, 0, 12),
                Child = стек
            });
        }
    }

    private void ОтрисоватьПомощь()
    {
        СписокПомощи.Children.Clear();

        if (данные!.Help.Count == 0)
        {
            СписокПомощи.Children.Add(new TextBlock
            {
                Text = "Вопросов пока нет — задайте первый!",
                Foreground = (Brush)FindResource("ТекстВторичный")
            });

            return;
        }

        foreach (var вопрос in данные.Help)
        {
            var стек = new StackPanel();

            стек.Children.Add(new TextBlock
            {
                Text = "❓ " + вопрос.Question,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            });

            стек.Children.Add(new TextBlock
            {
                Text = вопрос.Answered ? "💡 " + вопрос.Answer : "⏳ Ждёт ответа администратора или опытных игроков.",
                Foreground = вопрос.Answered
                    ? (Brush)FindResource("Успех")
                    : (Brush)FindResource("ТекстПриглушённый"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            стек.Children.Add(new TextBlock
            {
                Text = $"Спросил(а): {вопрос.AskedBy} • {вопрос.Time:dd.MM.yyyy}",
                Foreground = (Brush)FindResource("ТекстПриглушённый"),
                FontSize = 11,
                Margin = new Thickness(0, 6, 0, 0)
            });

            СписокПомощи.Children.Add(new Border
            {
                Style = (Style)FindResource("КарточкаМалая"),
                Margin = new Thickness(0, 0, 0, 12),
                Child = стек
            });
        }
    }

    private async void ОтправитьОтзыв_Click(object sender, RoutedEventArgs e)
    {
        var текст = ПолеНовогоОтзыва.Text.Trim();

        if (string.IsNullOrWhiteSpace(текст))
            return;

        ПолеНовогоОтзыва.Clear();

        await ЗапроситьAsync(new GameCommunityRequestDto
        {
            Action = GameCommunityAction.AddReview,
            GameName = игра.Название,
            ReviewText = текст
        });

        await ЗагрузитьAsync();
    }

    private async void ЗадатьВопрос_Click(object sender, RoutedEventArgs e)
    {
        var текст = ПолеВопроса.Text.Trim();

        if (string.IsNullOrWhiteSpace(текст))
            return;

        ПолеВопроса.Clear();

        await ЗапроситьAsync(new GameCommunityRequestDto
        {
            Action = GameCommunityAction.AskQuestion,
            GameName = игра.Название,
            QuestionText = текст
        });

        await ЗагрузитьAsync();
    }

    // Быстрая печать слева направо, вниз по строкам — не анимация WPF,
    // а таймер, дописывающий по 3 символа каждые 12 мс: TextBlock с
    // TextWrapping="Wrap" сам переносит строки по мере роста текста,
    // это и даёт нужный эффект "до самого низа".
    private void ЗапуститьАнимациюТекста(TextBlock цель, string полныйТекст)
    {
        таймерТекста?.Stop();

        индексТекста = 0;
        цель.Text = "";

        таймерТекста = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(12) };

        таймерТекста.Tick += (_, _) =>
        {
            индексТекста = Math.Min(полныйТекст.Length, индексТекста + 3);
            цель.Text = полныйТекст.Substring(0, индексТекста);

            if (индексТекста >= полныйТекст.Length)
                таймерТекста!.Stop();
        };

        таймерТекста.Start();
    }

    private void ПостроитьЗвёзды(StackPanel панель, int заполнено, Action<int> приКлике)
    {
        панель.Children.Clear();

        for (int i = 1; i <= 5; i++)
        {
            var номер = i;

            var звезда = new TextBlock
            {
                Text = номер <= заполнено ? "★" : "☆",
                FontSize = 30,
                Foreground = номер <= заполнено
                    ? (Brush)FindResource("Золото")
                    : (Brush)FindResource("ТекстПриглушённый"),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 4, 0)
            };

            звезда.MouseLeftButtonUp += (_, _) => приКлике(номер);

            панель.Children.Add(звезда);
        }
    }
}