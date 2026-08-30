using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreSteam;

public static class SteamGameImporter
{
    public static List<GameInfo> ImportAll()
    {
        var list = new List<GameInfo>();

        foreach (var manifest in SteamLibraryScanner.FindManifests())
        {
            var info =
                SteamManifestResolver.Read(manifest);

            if (info == null)
                continue;

            var steamApps =
                Directory.GetParent(manifest)!;

            var library =
                steamApps.Parent!;

            var exeFolder =
                Path.Combine(
                    steamApps.FullName,
                    "common",
                    info.InstallDir);

            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = info.Name,
                Category = "Steam",
                Path = exeFolder,
                Launcher = "Steam",
                AppId = info.AppId
            });
        }

        return list;
    }
}