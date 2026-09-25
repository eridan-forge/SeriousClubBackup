using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreScanners;

public static class RiotScanner
{
    // Новый API
    public static List<GameInfo> Scan()
    {
        return серьёзный.Core.CoreRiot.RiotScanner.Scan();
    }

    // Старый API (для GameFinder)
    public static List<InstalledGame> Find()
    {
        return Scan()
            .Select(game => new InstalledGame
            {
                Name = game.Name,
                Path = game.Path,
                Launcher = "Riot"
            })
            .ToList();
    }
}