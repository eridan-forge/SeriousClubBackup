using System.Text.RegularExpressions;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreSteam;

public static class SteamManifestResolver
{
    public static SteamGameInfo? Read(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return null;

        var text = File.ReadAllText(manifestPath);

        string Get(string key)
        {
            var match = Regex.Match(
                text,
                $"\"{key}\"\\s+\"([^\"]+)\"",
                RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value
                : "";
        }

        return new SteamGameInfo
        {
            AppId = Get("appid"),
            Name = Get("name"),
            InstallDir = Get("installdir"),
            BuildId = Get("buildid"),
            StateFlags = Get("StateFlags")
        };
    }
}