using System.Text.Json;
using серьёзный.Core.CoreConstants;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices
{
    public static class GameSyncService
    {
        public static void Send(
            int pcId,
            IEnumerable<GameEntry> games)
        {
            var packet =
                new GameSyncPacket
                {
                    PcId = pcId,
                    Games = games.ToList()
                };

            NetworkService.Broadcast(
                new NetworkPacket
                {
                    Type = MessageTypes.GamesUpdated,
                    PcId = pcId,
                    Json = JsonSerializer.Serialize(packet)
                });
        }

        public static void StartListening(
            Action<int, List<GameEntry>> receiver)
        {
            NetworkService.Start();

            NetworkEventHub.Message += packet =>
            {
                if (packet.Type != MessageTypes.GamesUpdated)
                    return;

                try
                {
                    var sync =
                        JsonSerializer.Deserialize<GameSyncPacket>(
                            packet.Json);

                    if (sync == null)
                        return;

                    receiver(sync.PcId, sync.Games);

                    foreach (var game in sync.Games)
                    {
                        CoreEvents.GameInstallEvent.Raise(
                            sync.PcId,
                            game);
                    }

                    GamesChangedEvent.Raise(sync.PcId);
                }
                catch
                {
                }
            };
        }
    }
}