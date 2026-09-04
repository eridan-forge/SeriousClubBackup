using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.Окна;

public partial class ОкноИгроки : Window
{
    private readonly Guid me;
    private readonly string моёИмя;

    private readonly DispatcherTimer обновление = new()
    {
        Interval = TimeSpan.FromSeconds(3)
    };

    private SocialStateDto? текущееСостояние;

    public ОкноИгроки(Guid myId, string моёИмя = "Игрок")
    {
        InitializeComponent();

        me = myId;
        this.моёИмя = моёИмя;

        Loaded += (_, _) =>
        {
            _ = ОбновитьAsync(new SocialActionDto { Action = SocialAction.GetState });

            обновление.Tick += (_, _) => _ = ОбновитьAsync(
                new SocialActionDto { Action = SocialAction.GetState });

            обновление.Start();
        };

        Closed += (_, _) => обновление.Stop();
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        Отрисовать();
    }

    private async Task ОбновитьAsync(SocialActionDto действие)
    {
        var requestId = SocialBridgeService.CreateRequest(me, действие);

        SocialStateDto? результат = null;

        for (int i = 0; i < 20; i++) // до ~6 сек
        {
            await Task.Delay(300);

            результат = SocialBridgeService.GetResult(requestId);

            if (результат != null)
                break;
        }

        if (результат == null)
            return; // сервер не ответил — оставляем текущий список без изменений

        текущееСостояние = результат;

        Отрисовать();
    }

    private void Отрисовать()
    {
        if (текущееСостояние == null)
            return;

        Players.Children.Clear();

        if (текущееСостояние.Incoming.Count > 0)
        {
            Players.Children.Add(СоздатьЗаголовокЗаявок(текущееСостояние.Incoming.Count));

            foreach (var заявка in текущееСостояние.Incoming)
            {
                Players.Children.Add(СоздатьКарточкуЗаявки(заявка));
            }

            Players.Children.Add(
                new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    Margin = new Thickness(0, 4, 0, 16)
                });
        }

        var text = Search.Text.Trim().ToLower();

        foreach (var player in текущееСостояние.Players)
        {
            if (!string.IsNullOrWhiteSpace(text) &&
                !player.FullName.ToLower().Contains(text))
                continue;

            Players.Children.Add(CreateCard(player));
        }
    }

    private UIElement СоздатьЗаголовокЗаявок(int count)
    {
        return new TextBlock
        {
            Text = $"📨 Заявки в друзья ({count})",
            Foreground = Brushes.White,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
    }

    private UIElement СоздатьКарточкуЗаявки(IncomingFriendRequestDto заявка)
    {
        var принять = new Button
        {
            Content = "✅ Принять",
            Width = 110,
            Height = 34,
            Margin = new Thickness(4)
        };

        принять.Click += (_, _) => _ = ОбновитьAsync(new SocialActionDto
        {
            Action = SocialAction.AcceptFriendRequest,
            RequestId = заявка.RequestId
        });

        var отклонить = new Button
        {
            Content = "✕ Отклонить",
            Width = 110,
            Height = 34,
            Margin = new Thickness(4)
        };

        отклонить.Click += (_, _) => _ = ОбновитьAsync(new SocialActionDto
        {
            Action = SocialAction.RemoveFriend,
            TargetId = заявка.FromAccountId
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        buttons.Children.Add(принять);
        buttons.Children.Add(отклонить);

        var row = new DockPanel();

        row.Children.Add(new TextBlock
        {
            Text = заявка.FromFullName,
            Foreground = Brushes.White,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        DockPanel.SetDock(buttons, Dock.Right);
        row.Children.Add(buttons);

        return new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromRgb(30, 58, 95)),
            Child = row
        };
    }

    private UIElement CreateCard(OnlinePlayerDto player)
    {
        var friend = new Button
        {
            Content = player.IsFriend
                ? "Удалить"
                : player.HasPendingOutgoing
                    ? "Заявка отправлена"
                    : "Добавить",

            IsEnabled = player.IsFriend || !player.HasPendingOutgoing,

            Width = 90,
            Margin = new Thickness(4)
        };

        friend.Click += (_, _) => _ = ОбновитьAsync(new SocialActionDto
        {
            Action = player.IsFriend
                ? SocialAction.RemoveFriend
                : SocialAction.SendFriendRequest,
            TargetId = player.AccountId
        });

        var block = new Button
        {
            Content = "Блок",
            Width = 90,
            Margin = new Thickness(4)
        };

        block.Click += (_, _) => _ = ОбновитьAsync(new SocialActionDto
        {
            Action = SocialAction.Block,
            TargetId = player.AccountId
        });

        var chatButton = new Button
        {
            Content = "💬",
            Width = 60,
            Margin = new Thickness(4)
        };

        chatButton.Click += (_, _) =>
        {
            new ОкноЛичногоЧатаИгрока(me, моёИмя, player.AccountId, player.FullName)
            {
                Owner = this
            }.Show();
        };

        DockPanel.SetDock(friend, Dock.Right);
        DockPanel.SetDock(block, Dock.Right);
        DockPanel.SetDock(chatButton, Dock.Right);

        var buttons = new DockPanel();

        buttons.Children.Add(friend);
        buttons.Children.Add(block);
        buttons.Children.Add(chatButton);

        var avatar = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(32),
            Background = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
            Margin = new Thickness(0, 0, 16, 0),

            Child = new TextBlock
            {
                Text = player.FullName.Substring(0, 1).ToUpper(),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            }
        };

        var info = new StackPanel();

        info.Children.Add(new TextBlock
        {
            Text = player.FullName,
            Foreground = Brushes.White,
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });

        info.Children.Add(new TextBlock
        {
            Text = player.Online
                ? $"🟢 ПК-{player.PcId:D2} • {player.CurrentGame ?? "В клубе"}"
                : "⚫ Не в сети",

            Foreground = player.Online ? Brushes.LimeGreen : Brushes.Gray
        });

        var top = new StackPanel { Orientation = Orientation.Horizontal };

        top.Children.Add(avatar);
        top.Children.Add(info);

        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(16),
            Cursor = Cursors.Hand,
            Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Child = new StackPanel { Children = { top, buttons } }
        };

        card.MouseLeftButtonUp += (_, _) =>
        {
            new ОкноПрофиляИгрока(player.AccountId)
            {
                Owner = this
            }.ShowDialog();
        };

        return card;
    }
}