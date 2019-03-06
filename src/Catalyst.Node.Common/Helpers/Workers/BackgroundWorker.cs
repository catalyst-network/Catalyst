using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class BackgroundWorker : IWorker, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingQueue<Action> _queue;

        public BackgroundWorker()
        {
            _queue = new BlockingQueue<Action>();
            _cancellationTokenSource = new CancellationTokenSource();
            Start();
        }

        public void Dispose() { Dispose(true); }

        public void Start()
        {
            Task.Factory.StartNew(() =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var action = _queue.Take();
                        action();
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Stop() { _cancellationTokenSource.Cancel(); }

        public void Queue(Action action) { _queue.Add(action); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
                _queue?.Dispose();
            }
        }
    }
}