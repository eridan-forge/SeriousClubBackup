using System.IO;

namespace серьёзный.Core.CoreServices
{
    public static class GameTransferService
    {
        public static string PrepareExecutable(
            int pcId,
            string exe)
        {
            var folder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "Games",
                    $"PC-{pcId}",
                    "Links");

            Directory.CreateDirectory(folder);

            var dest =
                Path.Combine(
                    folder,
                    Path.GetFileName(exe));

            File.Copy(
                exe,
                dest,
                true);

            return dest;
        }
    }
}