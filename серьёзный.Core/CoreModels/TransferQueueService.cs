using System.Collections.Concurrent;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreServices
{
    public static class TransferQueueService
    {
        private static readonly ConcurrentQueue<TransferJob> queue =
            new();

        public static void Enqueue(
            TransferJob job)
        {
            queue.Enqueue(job);
        }

        public static bool TryTake(
            out TransferJob job)
        {
            return queue.TryDequeue(
                out job!);
        }

        public static int Count =>
            queue.Count;
    }
}