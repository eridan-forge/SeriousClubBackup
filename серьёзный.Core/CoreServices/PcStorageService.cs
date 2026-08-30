using System;
using System.IO;

namespace серьёзный.Core.CoreServices
{
    public static class PcStorageService
    {
        public static string GetPcRoot(
            int pc)
        {
            var root =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    $"PC-{pc}");

            Directory.CreateDirectory(root);

            return root;
        }

        public static string GamesFolder(
            int pc)
        {
            var path =
                Path.Combine(
                    GetPcRoot(pc),
                    "Games");

            Directory.CreateDirectory(path);

            return path;
        }

        public static string ImagesFolder(
            int pc)
        {
            var path =
                Path.Combine(
                    GetPcRoot(pc),
                    "Images");

            Directory.CreateDirectory(path);

            return path;
        }

        public static string CacheFolder(
            int pc)
        {
            var path =
                Path.Combine(
                    GetPcRoot(pc),
                    "Cache");

            Directory.CreateDirectory(path);

            return path;
        }
    }
}