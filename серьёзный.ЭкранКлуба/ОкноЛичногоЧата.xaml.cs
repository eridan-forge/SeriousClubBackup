using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноЛичногоЧата : Window
{
    private readonly int pcId;
    private readonly string имяИгрока;

    private readonly DispatcherTimer обновление = new()
    {
        Interval = TimeSpan.FromSeconds(3)
    };

    private int показаноСообщений;

    public ОкноЛичногоЧата(int pcId, string имяИгрока)
    {
        InitializeComponent();

        this.pcId = pcId;
        this.имяИгрока = имяИгрока;

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
        var requestId = ChatHistoryBridgeService.CreateRequest(pcId);

        ChatHistoryDto? результат = null;

        for (int i = 0; i < 15; i++) // до ~4.5 сек
        {
            await Task.Delay(300);

            результат = ChatHistoryBridgeService.GetResult(requestId);

            if (результат != null)
                break;
        }

        if (результат == null)
            return;

        if (результат.Сообщения.Count == показаноСообщений)
            return; // ничего нового

        История.Children.Clear();

        foreach (var сообщение in результат.Сообщения)
        {
            ДобавитьПузырь(сообщение);
        }

        показаноСообщений = результат.Сообщения.Count;

        Скролл.ScrollToEnd();
    }

    private void ДобавитьПузырь(ChatMessageDto сообщение)
    {
        var пузырь = new Border
        {
            Background = сообщение.ОтАдминистратора
                ? new SolidColorBrush(Color.FromRgb(92, 24, 48))
                : new SolidColorBrush(Color.FromRgb(51, 65, 85)),

            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12),

            Margin = new Thickness(
                сообщение.ОтАдминистратора ? 50 : 0,
                4,
                сообщение.ОтАдминистратора ? 0 : 50,
                4),

            HorizontalAlignment = сообщение.ОтАдминистратора
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,

            MaxWidth = 330
        };

        var панель = new StackPanel();

        панель.Children.Add(new TextBlock
        {
            Text = сообщение.Имя,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        панель.Children.Add(new TextBlock
        {
            Text = сообщение.Текст,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });

        панель.Children.Add(new TextBlock
        {
            Text = сообщение.Время.ToString("HH:mm"),
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

        ChatOutboxBridgeService.CreateRequest(pcId, имяИгрока, текст);

        ПолеСообщения.Clear();

        ДобавитьПузырь(new ChatMessageDto
        {
            Имя = имяИгрока,
            Текст = текст,
            Время = DateTime.Now,
            ОтАдминистратора = false
        });

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