using System.Collections.Concurrent;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices
{
    public static class GameCacheService
    {
        private static readonly ConcurrentDictionary<
            int,
            List<GameEntry>> cache =
            new();

        public static void Store(
            int pcId,
            List<GameEntry> games)
        {
            cache[pcId] =
                games;
        }

        public static List<GameEntry> Get(
            int pcId)
        {
            if (cache.TryGetValue(
                    pcId,
                    out var games))
            {
                return games;
            }

            return new List<GameEntry>();
        }
    }
}