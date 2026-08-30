using серьёзный.Core.CoreShop;

namespace серьёзный.Core.CoreEvents;

public static class ShopRequestCompletedEvent
{
    public static event Action<ShopRequest>? Completed;

    public static void Notify(ShopRequest request)
    {
        Completed?.Invoke(request);
    }
}