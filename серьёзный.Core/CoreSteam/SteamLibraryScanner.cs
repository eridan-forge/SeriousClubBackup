using System.Text.RegularExpressions;
using System.IO;

namespace серьёзный.Core.CoreSteam;

public static class SteamLibraryScanner
{
    public static List<string> FindLibraries()
    {
        var result = new List<string>();

        var defaultPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Steam");

        Add(defaultPath);

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
                continue;

            Add(Path.Combine(
                drive.RootDirectory.FullName,
                "Steam"));

            Add(Path.Combine(
                drive.RootDirectory.FullName,
                "Program Files (x86)",
                "Steam"));

            Add(Path.Combine(
                drive.RootDirectory.FullName,
                "Games",
                "Steam"));
        }

        return result.Distinct().ToList();

        void Add(string path)
        {
            if (Directory.Exists(path))
                result.Add(path);
        }
    }

    public static IEnumerable<string> FindManifests()
    {
        foreach (var library in FindLibraries())
        {
            var steamApps =
                Path.Combine(library, "steamapps");

            if (!Directory.Exists(steamApps))
                continue;

            foreach (var file in Directory.GetFiles(
                steamApps,
                "appmanifest_*.acf"))
            {
                yield return file;
            }
        }
    }
}