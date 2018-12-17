using System;

namespace ADL.Node.Core.Modules.Network.Workers
{
    internal interface IWorker
    {
        void Stop();
        void Start();
        void Queue(Action action);
    }

    internal interface IWorkScheduler
    {
        void Start();
        void QueueForever(Action action, TimeSpan interval);
        void QueueOneTime(Action action, TimeSpan interval);
    }

}
