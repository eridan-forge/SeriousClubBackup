namespace серьёзный.Core.CoreShop;

public static class ShopSyncService
{
    public static event Action? Changed;

    public static void Notify()
    {
        Changed?.Invoke();
    }
}