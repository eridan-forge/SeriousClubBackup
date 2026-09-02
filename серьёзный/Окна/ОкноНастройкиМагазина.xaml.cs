using System.Windows;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreEvents;

namespace серьёзный.Окна;

public partial class ОкноНастройкиМагазина : Window
{
    private readonly ShopService shop =
        new();

    private bool загружается;

    public ОкноНастройкиМагазина()
    {
        InitializeComponent();

        ShopChangedEvent.Changed += МагазинИзменился;

        Closed += (_, _) =>
        {
            ShopChangedEvent.Changed -= МагазинИзменился;
        };

        ДобавитьРаздел.Click += ДобавитьРаздел_Click;
        ДобавитьТовар.Click += ДобавитьТовар_Click;

        Загрузить();
    }

    private void МагазинИзменился()
    {
        Dispatcher.Invoke(Загрузить);
    }

    private void Загрузить()
    {
        загружается = true;

        try
        {
            var categories =
                shop.GetCategories()
                    .OrderBy(x => x.Order)
                    .ToList();

            СписокРазделов.ItemsSource = categories;

            var settings =
                shop.GetSettings();

            ПоказатьМагазин.IsChecked =
                settings.Enabled;

            ПанельТоваров.Children.Clear();

            foreach (var item in shop.GetItems())
            {
                var card =
                    new Карточки.КарточкаТовара(item);

                card.DeleteRequested += id =>
                {
                    shop.DeleteItem(id);
                    Загрузить();
                };

                ПанельТоваров.Children.Add(card);
            }
        }
        finally
        {
            загружается = false;
        }
    }

    private void ПоказатьМагазин_Changed(
        object sender,
        RoutedEventArgs e)
    {
        // Checked/Unchecked срабатывает и когда мы сами выставляем
        // IsChecked внутри Загрузить() — в этом случае сохранять
        // и рассылать ShopChangedEvent не нужно.
        if (загружается)
            return;

        var settings =
            shop.GetSettings();

        settings.Enabled =
            ПоказатьМагазин.IsChecked == true;

        shop.SaveSettings(settings);
    }

    private void ДобавитьРаздел_Click(
    object? sender,
    RoutedEventArgs e)
    {
        var win =
            new ОкноСозданияРаздела
            {
                Owner = this
            };

        win.ShowDialog();
    }

    private void ДобавитьТовар_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (СписокРазделов.SelectedItem is not ShopCategory category)
        {
            MessageBox.Show(
                "Сначала выбери раздел.");

            return;
        }

        var win =
            new ОкноСозданияТовара(category.Id)
            {
                Owner = this
            };

        win.ShowDialog();
    }
}