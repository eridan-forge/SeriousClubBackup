using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreShop;
using серьёзный.Сервисы;

namespace серьёзный.Окна;

public partial class ОкноАктивныеЗаказы : Window
{
    private readonly ShopRequestService requests =
        new();

    private readonly СервисАккаунтов accounts = new();

    private readonly ShopService shop =
        new();

    public ОкноАктивныеЗаказы()
    {
        InitializeComponent();

        Loaded += (_, _) => Refresh();

        ShopLiveEvents.RequestCreated += _ =>
            Dispatcher.Invoke(Refresh);

        ShopLiveEvents.RequestUpdated += _ =>
            Dispatcher.Invoke(Refresh);
    }

    private void Refresh()
    {
        Orders.Children.Clear();

        var active =
            requests.All
                    .Where(x =>
                        x.Status != ShopRequestStatus.Completed &&
                        x.Status != ShopRequestStatus.Cancelled)
                    .OrderBy(x => x.Time)
                    .ToList();

        Badge.Text = active.Count.ToString();

        foreach (var request in active)
        {
            var item =
                shop.GetItems()
                    .FirstOrDefault(x => x.Id == request.ItemId);

            if (item == null)
                continue;

            Orders.Children.Add(CreateCard(request, item));
        }
    }

    private UIElement CreateCard(
        ShopRequest request,
        ShopItem item)
    {
        var row =
            new DockPanel();

        var preparing =
            new Button { Content = "Готовится" };

        preparing.Click += (_, _) =>
            requests.SetPreparing(request.Id);

        var ready =
            new Button { Content = "Готово" };

        ready.Click += (_, _) =>
            requests.SetReady(request.Id);

        var completed =
            new Button { Content = "✅ Выдано" };

        completed.Click += (_, _) =>
            requests.SetCompleted(request.Id);

        var cancel =
            new Button { Content = "✕" };

        cancel.Click += (_, _) =>
            requests.Cancel(request.Id);

        row.Children.Add(preparing);
        row.Children.Add(ready);
        row.Children.Add(completed);
        row.Children.Add(cancel);

        return new Border
        {
            Margin = new Thickness(0, 0, 0, 14),
            Padding = new Thickness(18),
            CornerRadius = new CornerRadius(16),
            Background =
                new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            Child =
                new StackPanel
                {
                    Children =
                    {
                        new TextBlock
{
    Text =
        $"ПК-{request.PcId:D2} • {(accounts.Получить(request.AccountId)?.ПолноеИмя ?? "Игрок")}",
    Foreground = Brushes.White,
    FontSize = 20,
    FontWeight = FontWeights.Bold
},
                        new TextBlock
                        {
                            Text =
                                $"{item.Name} • {item.Price:0} ₽",
                            Foreground = Brushes.LightGray
                        },

                        row
                    }
                }
        };
    }
}