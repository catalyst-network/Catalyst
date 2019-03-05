using System;
using System.Collections.Generic;
using System.Threading;

namespace Catalyst.Node.Common.Helpers.Workers
{
    internal class BlockingQueue<T> : IDisposable
    {
        public void Dispose()
        {
            _resetEvent?.Dispose();
        }

        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _queueLock = new object();
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }

        public T Take()
        {
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }
            }

            _resetEvent.WaitOne();

            return Take();
        }

        public void Add(T obj)
        {
            lock (_queueLock)
            {
                _queue.Enqueue(obj);
            }

            _resetEvent.Set();
        }
    }
}