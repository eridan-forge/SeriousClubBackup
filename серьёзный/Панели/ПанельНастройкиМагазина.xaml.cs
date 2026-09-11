using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreEvents;
using серьёзный.Окна;

namespace серьёзный.Панели;

public partial class ПанельНастройкиМагазина : UserControl
{
    private readonly ShopService shop =
        new();

    private bool загружается;

    public event Action? Закрыть;

    public ПанельНастройкиМагазина()
    {
        InitializeComponent();

        ShopChangedEvent.Changed += МагазинИзменился;

        Unloaded += (_, _) =>
        {
            ShopChangedEvent.Changed -= МагазинИзменился;
        };

        ДобавитьРаздел.Click += ДобавитьРаздел_Click;
        ДобавитьТовар.Click += ДобавитьТовар_Click;

        Загрузить();
    }

    private void Назад_Click(object sender, RoutedEventArgs e)
    {
        Закрыть?.Invoke();
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
                Owner = Window.GetWindow(this)
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
                Owner = Window.GetWindow(this)
            };

        win.ShowDialog();
    }
}