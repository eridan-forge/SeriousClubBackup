using System.Windows;
using серьёзный.Core.CoreShop;

namespace серьёзный.Окна;

public partial class ОкноВыбораПолучения : Window
{
    public ShopDeliveryType Result { get; private set; }

    public ОкноВыбораПолучения()
    {
        InitializeComponent();
    }

    private void Подойти_Click(object sender, RoutedEventArgs e)
    {
        Result = ShopDeliveryType.ComeToAdmin;

        DialogResult = true;
    }

    private void Принести_Click(object sender, RoutedEventArgs e)
    {
        Result = ShopDeliveryType.BringToPc;

        DialogResult = true;
    }
}