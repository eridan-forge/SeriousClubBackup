using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class AutoGameImporter
{
    public static List<GameInfo> ImportFolder(string folder)
    {
        var games = new List<GameInfo>();

        if (!Directory.Exists(folder))
            return games;

        foreach (var exe in Directory.EnumerateFiles(
            folder,
            "*.exe",
            SearchOption.AllDirectories))
        {
            var game = GameDetector.Detect(exe);

            games.Add(game);
        }

        return games
            .GroupBy(x => x.Name)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }
}