using серьёзный.Core.CoreChat;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using серьёзный.Core.CoreSocial;
using серьёзный.Сервисы;

namespace серьёзный.Окна;

public partial class ОкноИгроки : Window
{
    private readonly Guid me;

    private readonly SocialService social = new();
    private readonly СервисАккаунтов accounts = new();
    private readonly ChatService chat = new();

    public ОкноИгроки(Guid myId)
    {
        InitializeComponent();

        me = myId;

        Loaded += (_, _) => Refresh();
    }

    private void Search_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        Players.Children.Clear();

        // ---------------- ВХОДЯЩИЕ ЗАЯВКИ ----------------

        var входящие = social.Incoming(me);

        if (входящие.Count > 0)
        {
            Players.Children.Add(СоздатьЗаголовокЗаявок(входящие.Count));

            foreach (var заявка in входящие)
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

        // ---------------- СПИСОК ИГРОКОВ ----------------

        var text =
            Search.Text.Trim().ToLower();

        foreach (var account in accounts.ПолучитьВсе())
        {
            if (account.Id == me)
                continue;

            if (!string.IsNullOrWhiteSpace(text) &&
                !account.ПолноеИмя.ToLower().Contains(text))
                continue;

            Players.Children.Add(CreateCard(account));
        }
    }

    // =====================================================
    // ЗАГОЛОВОК БЛОКА ЗАЯВОК
    // =====================================================

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

    // =====================================================
    // КАРТОЧКА ОДНОЙ ЗАЯВКИ
    // =====================================================

    private UIElement СоздатьКарточкуЗаявки(FriendRelation заявка)
    {
        var отправитель =
            accounts.Получить(заявка.From);

        var имя =
            отправитель?.ПолноеИмя ?? "Неизвестный игрок";

        var принять =
            new Button
            {
                Content = "✅ Принять",
                Width = 110,
                Height = 34,
                Margin = new Thickness(4)
            };

        принять.Click += (_, _) =>
        {
            social.Accept(заявка.Id);
            Refresh();
        };

        var отклонить =
            new Button
            {
                Content = "✕ Отклонить",
                Width = 110,
                Height = 34,
                Margin = new Thickness(4)
            };

        отклонить.Click += (_, _) =>
        {
            social.Remove(заявка.From, заявка.To);
            Refresh();
        };

        var buttons =
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

        buttons.Children.Add(принять);
        buttons.Children.Add(отклонить);

        var row = new DockPanel();

        row.Children.Add(
            new TextBlock
            {
                Text = имя,
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
            Background =
                new SolidColorBrush(Color.FromRgb(30, 58, 95)),
            Child = row
        };
    }

    private UIElement CreateCard(dynamic account)
    {
        var state =
            social.Online.FirstOrDefault(x =>
                x.PlayerId == account.Id);

        var unread =
            chat.Unread(me);

        // ---------------- ДРУГ ----------------

        var friend =
            new Button
            {
                Content =
                    social.IsFriend(me, account.Id)
                        ? "Удалить"
                        : social.HasPending(me, account.Id)
                            ? "Заявка отправлена"
                            : "Добавить",

                IsEnabled =
                    social.IsFriend(me, account.Id) ||
                    !social.HasPending(me, account.Id),

                Width = 90,
                Margin = new Thickness(4)
            };

        friend.Click += (_, _) =>
        {
            if (social.IsFriend(me, account.Id))
                social.Remove(me, account.Id);
            else
                social.SendRequest(me, account.Id);

            Refresh();
        };

        // ---------------- БЛОК ----------------

        var block =
            new Button
            {
                Content =
                    social.IsBlocked(me, account.Id)
                        ? "Разблок"
                        : "Блок",

                Width = 90,
                Margin = new Thickness(4)
            };

        block.Click += (_, _) =>
        {
            if (social.IsBlocked(me, account.Id))
                social.Unblock(me, account.Id);
            else
                social.Block(me, account.Id);

            Refresh();
        };

        // ---------------- ЧАТ ----------------

        var chatButton =
            new Button
            {
                Content =
                    unread > 0
                        ? $"💬 {unread}"
                        : "💬",

                Width = 60,
                Margin = new Thickness(4)
            };

        chatButton.Click += (_, _) =>
        {
            var окно = new ОкноЧата
            {
                Owner = this
            };

            окно.ИмяАдминистратора = "Администратор";
            окно.УстановитьЛичныйЧат(
                account.Id,
                account.ПолноеИмя);

            окно.Show();
        };

        // ---------------- ГОЛОС ----------------

        var voice =
            new Button
            {
                Content = "🎙",
                Width = 50,
                Margin = new Thickness(4)
            };

        voice.Click += (_, _) =>
        {
            var окно = new ОкноЧата
            {
                Owner = this
            };

            окно.ИмяАдминистратора = "Администратор";
            окно.УстановитьЛичныйЧат(
                account.Id,
                account.ПолноеИмя);

            окно.Show();
        };

        DockPanel.SetDock(friend, Dock.Right);
        DockPanel.SetDock(block, Dock.Right);
        DockPanel.SetDock(chatButton, Dock.Right);
        DockPanel.SetDock(voice, Dock.Right);

        var buttons = new DockPanel();

        buttons.Children.Add(friend);
        buttons.Children.Add(block);
        buttons.Children.Add(chatButton);
        buttons.Children.Add(voice);

        // ---------------- АВАТАР ----------------

        var avatar =
            new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(32),
                Background =
                    new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                Margin = new Thickness(0, 0, 16, 0),

                Child =
                    new TextBlock
                    {
                        Text =
                            account.ПолноеИмя.Substring(0, 1).ToUpper(),

                        FontSize = 28,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
            };

        var info = new StackPanel();

        info.Children.Add(
            new TextBlock
            {
                Text = account.ПолноеИмя,
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeights.Bold
            });

        info.Children.Add(
            new TextBlock
            {
                Text =
                    state == null || !state.Online
                        ? "Не в сети"
                        : $"ПК-{state.PcId:D2} • {state.CurrentGame ?? "В клубе"}",

                Foreground =
                    state?.Online == true
                        ? Brushes.LimeGreen
                        : Brushes.Gray
            });

        var top =
            new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

        top.Children.Add(avatar);
        top.Children.Add(info);

        // ---------------- КАРТОЧКА ----------------

        var card =
            new Border
            {
                Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(16),
                Cursor = Cursors.Hand,

                Background =
                    new SolidColorBrush(Color.FromRgb(30, 41, 59)),

                Child =
                    new StackPanel
                    {
                        Children =
                        {
                            top,
                            buttons
                        }
                    }
            };

        card.MouseLeftButtonUp += (_, _) =>
        {
            new ОкноПрофиляИгрока(account.Id)
            {
                Owner = this
            }.ShowDialog();
        };

        return card;
    }
}