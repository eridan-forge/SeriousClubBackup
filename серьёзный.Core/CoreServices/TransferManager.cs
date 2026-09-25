using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices
{
    public static class TransferManager
    {
        public static void CreateJob(
            int targetPc,
            string gameId,
            string gameName,
            string exe,
            string image)
        {
            var job =
                new TransferJob
                {
                    TargetPc = targetPc,
                    GameId = gameId,
                    GameName = gameName,
                    SourceExe = exe,
                    SourceImage = image,
                    Size = new FileInfo(exe).Length
                };

            TransferQueueService.Enqueue(job);

             NetworkService.Client.Send(
                new NetworkPacket
                {
                    Type = "transfer.request",
                    PcId = targetPc,
                    Json = JsonSerializer.Serialize(job)
                },
                CoreConstants.NetworkConstants.Broadcast,
                CoreConstants.NetworkConstants.Port);
        }
    }
}