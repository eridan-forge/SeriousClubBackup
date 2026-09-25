using System;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreEvents
{
    public static class NetworkEventHub
    {
        public static event Action<NetworkPacket>? Message;

        public static void Publish(
            NetworkPacket packet)
        {
            Message?.Invoke(packet);
        }
    }
}