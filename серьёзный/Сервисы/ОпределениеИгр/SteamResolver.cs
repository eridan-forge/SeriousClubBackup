using System.Collections.Generic;

namespace серьёзный.Сервисы.ОпределениеИгр
{
    public static class SteamResolver
    {
        private static readonly Dictionary<string, string> ids =
            new()
            {
                ["Counter-Strike 2"] = "730",
                ["Dota 2"] = "570"
            };

        public static string? GetSteamUri(string game)
        {
            if (ids.TryGetValue(game, out var id))
            {
                return $"steam://run/{id}";
            }

            return null;
        }
    }
}