using System;

namespace Catalyst.Node.Modules.Core.P2P.Workers
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
