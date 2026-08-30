using System;
using System.IO;

namespace серьёзный.Core.CoreEvents
{
    public static class GamesChangedEvent
    {
        public static event Action<int>? Changed;

        public static void Raise(int pcId)
        {
            Changed?.Invoke(pcId);
        }
    }
}