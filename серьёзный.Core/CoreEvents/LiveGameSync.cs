namespace серьёзный.Core.CoreEvents;

using System.IO;

public static class LiveGameSync
{
    public static event Action<int>? Refresh;

    public static void Notify(int pcId)
    {
        Refresh?.Invoke(pcId);
    }
}