using System;
using System.IO;

namespace серьёзный.Core.CoreServices
{
    public static class GameImageService
    {
        public static string CopyToPcFolder(
            int pcId,
            string source)
        {
            var folder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "Games",
                    $"PC-{pcId}",
                    "Images");

            Directory.CreateDirectory(folder);

            var ext =
                Path.GetExtension(source);

            var name =
                Guid.NewGuid() + ext;

            var dest =
                Path.Combine(folder, name);

            File.Copy(
                source,
                dest,
                true);

            return dest;
        }
    }
}