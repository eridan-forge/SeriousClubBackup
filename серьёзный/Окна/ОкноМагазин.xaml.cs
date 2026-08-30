using System.Linq;
using System.Windows;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreShop;
using серьёзный.Карточки;

namespace серьёзный.Окна;

public partial class ОкноМагазин : Window
{
    private readonly ShopService shop = new();
    private readonly ShopRequestService requests = new();

    private readonly System.Guid accountId;
    private readonly int pcId;

    public ОкноМагазин(
    Guid accountId,
    int pcId)
    {
        InitializeComponent();

        this.accountId = accountId;
        this.pcId = pcId;

        ShopChangedEvent.Changed += МагазинИзменился;

        Closed += (_, _) =>
        {
            ShopChangedEvent.Changed -= МагазинИзменился;
        };

        СписокРазделов.SelectionChanged += (_, _) => ЗагрузитьТовары();

        Loaded += (_, _) => Загрузить();
    }

    private void МагазинИзменился()
    {
        Dispatcher.Invoke(Загрузить);
    }

    private void Загрузить()
    {
        var settings = shop.GetSettings();

        if (!settings.Enabled)
        {
            MessageBox.Show(
                "Магазин временно закрыт.");

            Close();
            return;
        }

        var categories =
            shop.GetCategories()
                .OrderBy(x => x.Order)
                .ToList();

        СписокРазделов.ItemsSource = categories;

        if (categories.Any())
            СписокРазделов.SelectedIndex = 0;
    }

    private void ЗагрузитьТовары()
    {
        ПанельТоваров.Children.Clear();

        if (СписокРазделов.SelectedItem is not ShopCategory category)
            return;

        foreach (var item in shop.GetItems().Where(x => x.CategoryId == category.Id))
        {
            if (item.Stock == 0)
                continue;

            var card = new КарточкаМагазина(item);

            card.BuyRequested += Купить;

            ПанельТоваров.Children.Add(card);
        }
    }

    private void Купить(ShopItem item)
    {
        var win = new ОкноВыбораПолучения()
        {
            Owner = this
        };

        if (win.ShowDialog() != true)
            return;

        requests.Create(
            accountId,
            pcId,
            item.Id,
            item.Name,
            item.Price,
            win.Result);

        MessageBox.Show(
            "Заявка отправлена администратору.");
    }
}