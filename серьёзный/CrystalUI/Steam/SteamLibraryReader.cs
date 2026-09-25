using System.Collections.Generic;
using System.IO;

namespace серьёзный.CrystalUI.Steam
{
    public static class SteamLibraryReader
    {
        public static List<string> GetLibraries()
        {
            var result =
                new List<string>();

            var file =
                Path.Combine(
                    System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.ProgramFilesX86),
                    "Steam",
                    "steamapps",
                    "libraryfolders.vdf");

            if (!File.Exists(file))
                return result;

            foreach (var line in File.ReadAllLines(file))
            {
                if (!line.Contains("path"))
                    continue;

                var split =
                    line.Split('"');

                if (split.Length > 5)
                {
                    result.Add(
                        Path.Combine(
                            split[5],
                            "steamapps"));
                }
            }

            return result;
        }
    }
}