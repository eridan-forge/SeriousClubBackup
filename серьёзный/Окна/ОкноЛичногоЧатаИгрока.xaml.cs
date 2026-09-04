using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.Окна;

public partial class ОкноЛичногоЧатаИгрока : Window
{
    private readonly Guid me;
    private readonly Guid friend;
    private readonly string моёИмя;

    private readonly DispatcherTimer обновление = new()
    {
        Interval = TimeSpan.FromSeconds(3)
    };

    private int показаноСообщений;

    public ОкноЛичногоЧатаИгрока(Guid me, string моёИмя, Guid friend, string friendName)
    {
        InitializeComponent();

        this.me = me;
        this.моёИмя = моёИмя;
        this.friend = friend;

        Заголовок.Text = friendName;

        Loaded += (_, _) =>
        {
            ЗагрузитьИсторию();

            обновление.Tick += (_, _) => ЗагрузитьИсторию();
            обновление.Start();
        };

        Closed += (_, _) => обновление.Stop();
    }

    private async void ЗагрузитьИсторию()
    {
        var requestId = PlayerChatHistoryBridgeService.CreateRequest(me, friend);

        PlayerChatHistoryDto? результат = null;

        for (int i = 0; i < 15; i++) // до ~4.5 сек
        {
            await Task.Delay(300);

            результат = PlayerChatHistoryBridgeService.GetResult(requestId);

            if (результат != null)
                break;
        }

        if (результат == null)
            return;

        if (результат.Messages.Count == показаноСообщений)
            return;

        История.Children.Clear();

        foreach (var сообщение in результат.Messages)
        {
            ДобавитьПузырь(сообщение.FromName, сообщение.Text, сообщение.Time, сообщение.From == me);
        }

        показаноСообщений = результат.Messages.Count;

        Скролл.ScrollToEnd();
    }

    private void ДобавитьПузырь(string имя, string текст, DateTime время, bool моё)
    {
        var пузырь = new Border
        {
            Background = моё
                ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                : new SolidColorBrush(Color.FromRgb(51, 65, 85)),

            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12),
            Margin = new Thickness(моё ? 50 : 0, 4, моё ? 0 : 50, 4),

            HorizontalAlignment = моё
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,

            MaxWidth = 300
        };

        var панель = new StackPanel();

        панель.Children.Add(new TextBlock
        {
            Text = имя,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        панель.Children.Add(new TextBlock
        {
            Text = текст,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });

        панель.Children.Add(new TextBlock
        {
            Text = время.ToString("HH:mm"),
            Foreground = Brushes.LightGray,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        });

        пузырь.Child = панель;

        История.Children.Add(пузырь);
    }

    private void Отправить_Click(object sender, RoutedEventArgs e)
    {
        var текст = ПолеСообщения.Text.Trim();

        if (string.IsNullOrWhiteSpace(текст))
            return;

        PlayerChatOutboxBridgeService.CreateRequest(me, friend, моёИмя, текст);

        ПолеСообщения.Clear();

        ДобавитьПузырь(моёИмя, текст, DateTime.Now, true);

        показаноСообщений++;

        Скролл.ScrollToEnd();
    }

    private void ПолеСообщения_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (Keyboard.Modifiers == ModifierKeys.Shift)
            return;

        e.Handled = true;

        Отправить_Click(this, new RoutedEventArgs());
    }
}