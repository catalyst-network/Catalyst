using System;

namespace Catalyst.Node.Modules.Core.P2P.Workers
{
    internal class ClientWorker : IWorker, IWorkScheduler
    {
        private readonly BackgroundWorker _backgroundWorker;
        private readonly TimedWorker _timedWorker;

        /// <summary>
        /// </summary>
        public ClientWorker()
        {
            _timedWorker = new TimedWorker();
            _backgroundWorker = new BackgroundWorker();
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        public void Queue(Action action)
        {
            _backgroundWorker.Queue(action);
        }

        /// <summary>
        /// </summary>
        public void Start()
        {
            _backgroundWorker.Start();
            _timedWorker.Start();
        }

        /// <summary>
        /// </summary>
        public void Stop()
        {
            _timedWorker.Stop();
            _backgroundWorker.Stop();
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueForever(Action action, TimeSpan interval)
        {
            _timedWorker.QueueForever(() => _backgroundWorker.Queue(action), interval);
        }

        /// <summary>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="interval"></param>
        public void QueueOneTime(Action action, TimeSpan interval)
        {
            _timedWorker.QueueOneTime(() => _backgroundWorker.Queue(action), interval);
        }
    }
}