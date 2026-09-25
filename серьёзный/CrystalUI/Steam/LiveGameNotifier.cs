using System;

namespace серьёзный.CrystalUI.Steam
{
    public static class LiveGameNotifier
    {
        public static event Action<int>? Refresh;

        public static void Raise(int pc)
        {
            Refresh?.Invoke(pc);
        }
    }
}