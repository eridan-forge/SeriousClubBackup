using System;

namespace серьёзный.Core.CoreEvents
{
    public static class TransferProgressEvent
    {
        public static event Action<Guid, double>? Progress;

        public static event Action<Guid>? Finished;

        public static event Action<Guid>? Failed;

        public static void RaiseProgress(
            Guid job,
            double percent)
        {
            Progress?.Invoke(job, percent);
        }

        public static void RaiseFinished(
            Guid job)
        {
            Finished?.Invoke(job);
        }

        public static void RaiseFailed(
            Guid job)
        {
            Failed?.Invoke(job);
        }
    }
}