using серьёзный.Core.CoreShop;

namespace серьёзный.Core.CoreEvents;

public static class ShopRequestEvent
{
    public static event Action<ShopRequest>? Created;

    public static void Notify(ShopRequest request)
    {
        Created?.Invoke(request);
    }
}