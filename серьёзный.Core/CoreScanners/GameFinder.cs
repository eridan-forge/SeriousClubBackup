using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreScanners;

public static class GameFinder
{
    public static List<InstalledGame> FindAll()
    {
        var игры = new List<InstalledGame>();

        игры.AddRange(SteamScanner.Find());
        игры.AddRange(EpicScanner.Find());
        игры.AddRange(RiotScanner.Find());
        игры.AddRange(RegistryScanner.Find());

        return игры
            .GroupBy(x => x.Path.ToLower())
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }
}