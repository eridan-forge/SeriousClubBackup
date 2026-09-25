using System.Text.RegularExpressions;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class SteamLibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var result = new List<GameInfo>();

        var libraries =
            GetLibraries();

        foreach (var library in libraries)
        {
            var steamapps =
                Path.Combine(library, "steamapps");

            if (!Directory.Exists(steamapps))
                continue;

            foreach (var manifest in Directory.GetFiles(
                         steamapps,
                         "appmanifest_*.acf"))
            {
                try
                {
                    var text =
                        File.ReadAllText(manifest);

                    var name =
                        ReadValue(text, "name");

                    var appId =
                        ReadValue(text, "appid");

                    var folder =
                        ReadValue(text, "installdir");

                    var install =
                        Path.Combine(
                            steamapps,
                            "common",
                            folder);

                    result.Add(new GameInfo
                    {
                        Name = name,
                        Category = "Steam",
                        Path = install,
                        Launcher = "Steam",
                        AppId = appId
                    });
                }
                catch
                {
                }
            }
        }

        return result;
    }

    private static List<string> GetLibraries()
    {
        var list = new List<string>();

        var file =
            SteamDetector.GetLibraryFolders();

        if (file == null)
            return list;

        var text =
            File.ReadAllText(file);

        foreach (Match m in Regex.Matches(
                     text,
                     "\"path\"\\s*\"([^\"]+)\""))
        {
            list.Add(
                m.Groups[1].Value.Replace(@"\\", @"\"));
        }

        return list;
    }

    private static string ReadValue(
        string text,
        string key)
    {
        var m =
            Regex.Match(
                text,
                $"\"{key}\"\\s*\"([^\"]+)\"");

        return m.Success
            ? m.Groups[1].Value
            : "";
    }
}