using серьёзный.Core.CoreLauncher;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class UniversalGameImportService
{
    public static List<GameInfo> ScanComputer()
    {
        var games =
            LauncherUniversalScanner.ScanAll();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            try
            {
                games.AddRange(
                    AutoGameImporter.ImportFolder(
                        drive.RootDirectory.FullName));
            }
            catch
            {
            }
        }

        return games
            .GroupBy(x => x.Name)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }
}