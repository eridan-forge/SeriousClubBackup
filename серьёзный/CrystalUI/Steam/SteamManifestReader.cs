using System.IO;

namespace серьёзный.CrystalUI.Steam
{
    public static class SteamManifestReader
    {
        public static string GetName(string manifest)
        {
            foreach (var line in File.ReadAllLines(manifest))
            {
                if (line.Contains("\"name\""))
                {
                    var split =
                        line.Split('"');

                    if (split.Length > 3)
                        return split[3];
                }
            }

            return Path.GetFileNameWithoutExtension(manifest);
        }
    }
}