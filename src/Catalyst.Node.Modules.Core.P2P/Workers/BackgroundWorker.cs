using System;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Node.Modules.Core.P2P.Workers
{
    internal class BackgroundWorker : IWorker
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingQueue<Action> _queue;

        public BackgroundWorker()
        {
            _queue = new BlockingQueue<Action>();
            _cancellationTokenSource = new CancellationTokenSource();
            Start();
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var action = _queue.Take();
                        action();
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Queue(Action action)
        {
            _queue.Add(action);
        }
    }
}
