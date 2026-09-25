using System.Net;
using System.Net.Sockets;
using System.Text;

namespace серьёзный.Патруль.Сеть
{
    public static class ПоискСервера
    {
        public static async Task<(string ip, int порт)?>
            НайтиAsync(
                CancellationToken токен)
        {
            using var udp =
                new UdpClient(47820);

            while (!токен.IsCancellationRequested)
            {
                var результат =
                    await udp.ReceiveAsync();

                var текст =
                    Encoding.UTF8.GetString(
                        результат.Buffer);

                if (!текст.StartsWith(
                        "SERIOUS_SERVER|"))
                    continue;

                var части =
                    текст.Split('|');

                return (
                    части[1],
                    int.Parse(части[2]));
            }

            return null;
        }
    }
}