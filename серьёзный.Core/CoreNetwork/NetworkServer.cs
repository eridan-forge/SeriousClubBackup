using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreLogs;

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
            try
            {
                server = new UdpClient(port);

                server.EnableBroadcast = true;

                BeginReceive();
            }
            catch (Exception ошибка)
            {

                // Порт уже занят (второй запущенный экземпляр процесса —
                                // частый случай, т.к. окно ЭкранКлуба нельзя закрыть
                                // крестиком и его не видно в трее). Раньше SocketException
                                // вылетал отсюда наружу и обрывал ПриЗагрузке целиком —
                                // ни часы, ни опрос состояния, ни живая смена темы дальше
                                // не запускались. Эта фича не критична — просто логируем.
                server = null;
                
                LaunchLogger.Write(
                $"NetworkServer: не удалось занять порт {port}: {ошибка.Message}");
            }
            

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