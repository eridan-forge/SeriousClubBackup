using серьёзный.Core.CoreConstants;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreNetwork;
using System.IO;

namespace серьёзный.Core.CoreServices
{
    public static class NetworkService
    {
        public static NetworkServer Server { get; } =
            new();

        public static NetworkClient Client { get; } =
            new();

        public static void Start()
        {
            if (Server.IsRunning)
                return;

            Server.PacketReceived += packet =>
            {
                NetworkEventHub.Publish(packet);
            };

            Server.Start(
                NetworkConstants.Port);
        }

        public static void Broadcast(
            NetworkPacket packet)
        {
            Client.Send(
                packet,
                NetworkConstants.Broadcast,
                NetworkConstants.Port);
        }
    }
}