using System.IO;
using System.Security.Cryptography;

namespace серьёзный.Core.CoreServices
{
    public static class GameIntegrityService
    {
        public static string HashFile(
            string path)
        {
            using var sha =
                SHA256.Create();

            using var stream =
                File.OpenRead(path);

            var hash =
                sha.ComputeHash(stream);

            return Convert.ToHexString(hash);
        }

        public static bool Exists(
            string path)
        {
            return File.Exists(path);
        }
    }
}