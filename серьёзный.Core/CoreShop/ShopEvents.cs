namespace серьёзный.Core.CoreShop;

public static class ShopEvents
{
    public static event Action<ShopOrderNotification>? OrderRequested;

    public static void RaiseOrder(ShopOrderNotification notification)
    {
        OrderRequested?.Invoke(notification);
    }
}