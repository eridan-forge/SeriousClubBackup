using System;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreEvents
{
    public static class GameInstallEvent
    {
        public static event Action<int, GameEntry>? InstallRequested;

        public static void Raise(
            int pcId,
            GameEntry game)
        {
            InstallRequested?.Invoke(pcId, game);
        }
    }
}