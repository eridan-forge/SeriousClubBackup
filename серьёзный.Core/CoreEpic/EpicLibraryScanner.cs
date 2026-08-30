using серьёзный.Core.CoreModels;
using System.Text.Json;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreEpic;

public static class EpicLibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        var file =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "Epic",
                "EpicGamesLauncher",
                "Data",
                "LauncherInstalled.dat");

        if (!File.Exists(file))
            return list;

        using var stream =
            File.OpenRead(file);

        using var json =
            JsonDocument.Parse(stream);

        if (!json.RootElement.TryGetProperty(
                "InstallationList",
                out var installs))
            return list;

        foreach (var game in installs.EnumerateArray())
        {
            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = game.GetProperty("AppName").GetString() ?? "",
                Category = "Epic",
                Path = game.GetProperty("InstallLocation").GetString() ?? "",
                Launcher = "Epic"
            });
        }

        return list;
    }
}