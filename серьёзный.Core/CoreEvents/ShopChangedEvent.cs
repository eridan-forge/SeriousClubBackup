namespace серьёзный.Core.CoreEvents;

public static class ShopChangedEvent
{
    public static event Action? Changed;

    public static void Notify()
    {
        Changed?.Invoke();
    }
}