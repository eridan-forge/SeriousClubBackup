using System.Windows;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreShop;

namespace серьёзный.Уведомления;

public class ShopToastManager
{
    private readonly ShopService shop =
        new();

    private readonly ShopService магазин =
    new();

    public ShopToastManager()
    {
        ShopRequestEvent.Created += Show;

        ShopLiveEvents.RequestUpdated += ShowStatus;
    }



    private void Show(ShopRequest request)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var item =
    магазин.GetItems()
           .FirstOrDefault(x => x.Id == request.ItemId);

            if (item == null)
                return;

            var toast =
                new ShopToast(request, item);

            toast.Show();
        });

    }

    private void ShowStatus(ShopRequest request)
    {
        if (request.Status != ShopRequestStatus.Ready)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                request.Delivery ==
                ShopDeliveryType.BringToPc
                    ? "🚶 Администратор уже несёт заказ к вашему ПК."
                    : "🧋 Ваш заказ готов. Подойдите к администратору.",
                "Заказ готов");
        });
    }


}