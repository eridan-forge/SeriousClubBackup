using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreNetwork
{
    public class NetworkServer
    {
        private UdpClient? server;

        public bool IsRunning =>
            server != null;

        public event Action<NetworkPacket>? PacketReceived;

        public void Start(int port)
        {
            if (server != null)
                return;

            server = new UdpClient(port);

            server.EnableBroadcast = true;

            BeginReceive();
        }

        public void Stop()
        {
            server?.Close();
            server = null;
        }

        private async void BeginReceive()
        {
            while (server != null)
            {
                try
                {
                    var result =
                        await server.ReceiveAsync();

                    var json =
                        Encoding.UTF8.GetString(
                            result.Buffer);

                    var packet =
                        JsonSerializer.Deserialize<NetworkPacket>(
                            json);

                    if (packet != null)
                        PacketReceived?.Invoke(packet);
                }
                catch
                {
                    break;
                }
            }
        }

        public async void Send(
            NetworkPacket packet,
            string address,
            int port)
        {
            if (server == null)
                return;

            var json =
                JsonSerializer.Serialize(packet);

            var bytes =
                Encoding.UTF8.GetBytes(json);

            await server.SendAsync(
                bytes,
                bytes.Length,
                address,
                port);
        }
    }
}