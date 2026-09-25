using System.IO;
using System.Net.Http;

namespace серьёзный.Core.CoreSteam;

public static class SteamCoverDownloader
{
    private static readonly HttpClient client = new();

    public static async Task<string?> DownloadAsync(
        string appId,
        string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);

            var file =
                Path.Combine(folder, $"{appId}.jpg");

            if (File.Exists(file))
                return file;

            var url =
                $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900.jpg";

            var bytes =
                await client.GetByteArrayAsync(url);

            await File.WriteAllBytesAsync(file, bytes);

            return file;
        }
        catch
        {
            return null;
        }
    }
}