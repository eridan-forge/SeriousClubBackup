using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using серьёзный.Core.CoreChat;
using серьёзный.Модели;

namespace серьёзный.Окна;

public partial class ОкноЧата : Window
{
    // ==========================================
    // Чат администратора
    // ==========================================

    public event Action<string, string>? СообщениеОтправлено;
    public event Action<long>? СообщениеУдалить;

    public string ИмяАдминистратора { get; set; } = "Администратор";

    // ==========================================
    // Личный чат
    // ==========================================

    private readonly ChatService chat = new();

    // "Кем" является это окно в диалоге.
    // По умолчанию Guid.Empty — так работает окно
    // со стороны администратора (не менялось).
    // Окно игрока перед вызовом УстановитьЛичныйЧат
    // должно установить сюда Id своего аккаунта.
    public Guid МойId { get; set; } = Guid.Empty;

    private Guid собеседникId;
    private string имяСобеседника = "";
    private bool личныйЧат;

    public ОкноЧата()
    {
        InitializeComponent();

        Closed += (_, _) =>
        {
            ChatLiveEvents.MessageReceived -= ПолученоЛичноеСообщение;
        };
    }

    // ==========================================
    // Запуск личного чата
    // ==========================================

    public void УстановитьЛичныйЧат(
        Guid аккаунтId,
        string имя)
    {
        личныйЧат = true;

        собеседникId = аккаунтId;
        имяСобеседника = имя;

        Title = $"Чат — {имя}";
        Заголовок.Text = имя;
        Непрочитанные.Text = "";

        ОчиститьИсторию();

        foreach (var msg in chat.Get(МойId, собеседникId))
        {
            ДобавитьСообщение(
                new ЗаписьЧата
                {
                    Имя =
                        msg.From == МойId
                            ? ИмяАдминистратора
                            : имяСобеседника,

                    Текст = msg.Text,
                    Время = msg.Time,
                    ОтАдминистратора = msg.From == МойId,
                    АккаунтGuid = собеседникId,
                    Прочитано = true
                });
        }

        chat.MarkRead(МойId, собеседникId);

        ChatLiveEvents.MessageReceived -= ПолученоЛичноеСообщение;
        ChatLiveEvents.MessageReceived += ПолученоЛичноеСообщение;
    }

    // ==========================================
    // Старый режим
    // ==========================================

    public void ОчиститьИсторию()
    {
        История.Children.Clear();
    }

    public void УстановитьКомпьютер(
        string название,
        int непрочитанные)
    {
        Заголовок.Text = название;

        Непрочитанные.Text =
            непрочитанные > 0
                ? $"🔴 {непрочитанные}"
                : "";
    }

    public void ДобавитьСообщение(
        ЗаписьЧата сообщение)
    {
        var пузырь =
            new Border
            {
                Background =
                    сообщение.ОтАдминистратора
                        ? new SolidColorBrush(Color.FromRgb(92, 24, 48))
                        : new SolidColorBrush(Color.FromRgb(51, 65, 85)),

                CornerRadius = new CornerRadius(14),

                Padding = new Thickness(12),

                Margin =
                    new Thickness(
                        сообщение.ОтАдминистратора ? 50 : 0,
                        4,
                        сообщение.ОтАдминистратора ? 0 : 50,
                        4),

                HorizontalAlignment =
                    сообщение.ОтАдминистратора
                        ? HorizontalAlignment.Right
                        : HorizontalAlignment.Left,

                MaxWidth = 330
            };

        var панель = new StackPanel();

        панель.Children.Add(
            new TextBlock
            {
                Text = сообщение.Имя,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

        панель.Children.Add(
            new TextBlock
            {
                Text = сообщение.Текст,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

        панель.Children.Add(
            new TextBlock
            {
                Text = сообщение.ВремяСтрокой,
                Foreground = Brushes.LightGray,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right
            });

        пузырь.Child = панель;

        var меню = new ContextMenu();

        var удалить =
            new MenuItem
            {
                Header = "Удалить сообщение"
            };

        удалить.Click += (_, _) =>
        {
            СообщениеУдалить?.Invoke(сообщение.Id);
        };

        меню.Items.Add(удалить);

        пузырь.ContextMenu = меню;

        История.Children.Add(пузырь);

        НайтиScrollViewer()?.ScrollToEnd();
    }

    // ==========================================
    // Пришло новое личное сообщение
    // ==========================================

    private void ПолученоЛичноеСообщение(
        ChatMessage msg)
    {
        if ((msg.From != собеседникId || msg.To != МойId) &&
            (msg.From != МойId || msg.To != собеседникId))
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            ДобавитьСообщение(
                new ЗаписьЧата
                {
                    Имя =
                        msg.From == МойId
                            ? ИмяАдминистратора
                            : имяСобеседника,

                    Текст = msg.Text,
                    Время = msg.Time,
                    ОтАдминистратора = msg.From == МойId,
                    АккаунтGuid = собеседникId,
                    Прочитано = true
                });

            chat.MarkRead(МойId, собеседникId);
        });
    }

    // ==========================================
    // Отправка
    // ==========================================

    private void Отправить_Click(
        object sender,
        RoutedEventArgs e)
    {
        var текст =
            ПолеСообщения.Text.Trim();

        if (string.IsNullOrWhiteSpace(текст))
            return;

        if (личныйЧат)
        {
            // Сообщение само появится через ChatLiveEvents.
            chat.Send(
                МойId,
                собеседникId,
                текст);
        }
        else
        {
            СообщениеОтправлено?.Invoke(
                ИмяАдминистратора,
                текст);

            ДобавитьСообщение(
                new ЗаписьЧата
                {
                    Имя = ИмяАдминистратора,
                    Текст = текст,
                    Время = DateTime.Now,
                    ОтАдминистратора = true,
                    Прочитано = true
                });
        }

        ПолеСообщения.Clear();
    }

    private void ПолеСообщения_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (Keyboard.Modifiers == ModifierKeys.Shift)
            return;

        e.Handled = true;

        Отправить_Click(
            this,
            new RoutedEventArgs());
    }

    // ==========================================
    // ScrollViewer
    // ==========================================

    private ScrollViewer? НайтиScrollViewer()
    {
        if (Content is not DependencyObject root)
            return null;

        return НайтиVisualChild<ScrollViewer>(root);
    }

    private static T? НайтиVisualChild<T>(
        DependencyObject obj)
        where T : DependencyObject
    {
        for (int i = 0;
             i < VisualTreeHelper.GetChildrenCount(obj);
             i++)
        {
            var child =
                VisualTreeHelper.GetChild(obj, i);

            if (child is T found)
                return found;

            var next =
                НайтиVisualChild<T>(child);

            if (next != null)
                return next;
        }

        return null;
    }
}