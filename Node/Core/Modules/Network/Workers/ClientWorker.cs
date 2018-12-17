using System;

namespace ADL.Node.Core.Modules.Network.Workers
{
    class ClientWorker : IWorker, IWorkScheduler
    {
        private readonly TimedWorker _timedWorker;
        private readonly BackgroundWorker _backgroundWorker;

        public ClientWorker()
        {
            _timedWorker = new TimedWorker();
            _backgroundWorker = new BackgroundWorker();
        }

        public void Queue(Action action)
        {
            _backgroundWorker.Queue(action);
        }

        public void QueueForever(Action action, TimeSpan interval)
        {
            _timedWorker.QueueForever(() => _backgroundWorker.Queue(action), interval);
        }

        public void QueueOneTime(Action action, TimeSpan interval)
        {
            _timedWorker.QueueOneTime(() => _backgroundWorker.Queue(action), interval);
        }

        public void Start()
        {
            _backgroundWorker.Start();
            _timedWorker.Start();
        }

        public void Stop()
        {
            _timedWorker.Stop();
            _backgroundWorker.Stop();
        }
    }
}
