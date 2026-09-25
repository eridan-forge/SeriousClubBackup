using System.Windows;
using System.Windows.Threading;
using серьёзный.Core.CoreShop;
using серьёзный.Сервисы;

namespace серьёзный.Уведомления;

public partial class ShopToast : Window
{
    private readonly DispatcherTimer timer = new();

    public ShopToast(ShopRequest request, ShopItem item)
    {
        InitializeComponent();

        var accounts = new СервисАккаунтов();

        var account = accounts.Получить(request.AccountId);

        var name = account?.ПолноеИмя ?? "Игрок";

        Текст.Text =
    $"ПК-{request.PcId:D2} • {name}\n" +
    $"{item.Name} • {item.Price:0} ₽\n" +
    (request.Delivery == ShopDeliveryType.BringToPc
        ? "🚶 Принести к ПК"
        : "🧍 Подойдёт к администратору");

        Loaded += (_, _) =>
        {
            Left =
                SystemParameters.WorkArea.Right - Width - 20;

            Top =
                SystemParameters.WorkArea.Bottom - Height - 20;
        };

        Готово.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        Закрыть.Click += (_, _) =>
        {
            Close();
        };
    }
}