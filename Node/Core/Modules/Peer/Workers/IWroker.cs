using System;

namespace ADL.Node.Core.Modules.Peer.Workers
{
 
    internal interface IWorker
    {
        void Queue(Action action);
        void Start();
        void Stop();
    }

    internal interface IWorkScheduler
    {
        void QueueForever(Action action, TimeSpan interval);
        void QueueOneTime(Action action, TimeSpan interval);
    }
}