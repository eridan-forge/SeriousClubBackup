using System.IO;
using System.Net.Http;

namespace серьёзный.CrystalUI.Steam
{
    public static class SteamArtworkService
    {
        private static readonly HttpClient http =
            new();

        public static async Task DownloadCover(
            string appId,
            string save)
        {
            var url =
                $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900.jpg";

            Directory.CreateDirectory(
                Path.GetDirectoryName(save)!);

            var bytes =
                await http.GetByteArrayAsync(url);

            await File.WriteAllBytesAsync(save, bytes);
        }
    }
}