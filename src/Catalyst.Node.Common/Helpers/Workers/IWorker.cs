using System;

namespace Catalyst.Node.Common.Helpers.Workers
{
    public interface IWorker
    {
        void Stop();
        void Start();
        void Queue(Action action);
    }

    public interface IWorkScheduler
    {
        void Start();
        void QueueForever(Action action, TimeSpan interval);
        void QueueOneTime(Action action, TimeSpan interval);
    }
}