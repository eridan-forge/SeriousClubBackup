using System.IO;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices
{
    public class TransferWorker
    {
        public void Start()
        {
            Task.Run(Loop);
        }

        private async Task Loop()
        {
            while (true)
            {
                if (TransferQueueService.TryTake(out var job))
                {
                    await Send(job);
                }

                await Task.Delay(100);
            }
        }

        private async Task Send(
            CoreModels.TransferJob job)
        {
            var header =
                new FileTransferHeader
                {
                    JobId = job.JobId,
                    FileName = Path.GetFileName(job.SourceImage),
                    GameId = job.GameId,
                    Size = new FileInfo(job.SourceImage).Length,
                    IsImage = true
                };

            NetworkService.Client.Send(
                new NetworkPacket
                {
                    Type = "transfer.begin",
                    PcId = job.TargetPc,
                    Json = System.Text.Json.JsonSerializer.Serialize(header)
                },
                CoreConstants.NetworkConstants.Broadcast,
                CoreConstants.NetworkConstants.Port);

            await Task.Delay(50);
        }
    }
}