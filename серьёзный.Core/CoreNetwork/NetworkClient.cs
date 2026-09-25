using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreNetwork
{
    public class NetworkClient
    {
        private readonly UdpClient client =
            new();

        public NetworkClient()
        {
            client.EnableBroadcast = true;
        }

        public async Task Send(
            NetworkPacket packet,
            string address,
            int port)
        {
            var json =
                JsonSerializer.Serialize(packet);

            var bytes =
                Encoding.UTF8.GetBytes(json);

            await client.SendAsync(
                bytes,
                bytes.Length,
                address,
                port);
        }
    }
}