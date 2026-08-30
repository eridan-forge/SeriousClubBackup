using серьёзный.Core.CoreShop;

namespace серьёзный.Core.CoreEvents;

public static class ShopLiveEvents
{
    public static event Action? ShopChanged;

    public static event Action<ShopRequest>? RequestCreated;

    public static event Action<ShopRequest>? RequestUpdated;

    public static void NotifyShopChanged()
        => ShopChanged?.Invoke();

    public static void NotifyCreated(ShopRequest request)
        => RequestCreated?.Invoke(request);

    public static void NotifyUpdated(ShopRequest request)
        => RequestUpdated?.Invoke(request);
}