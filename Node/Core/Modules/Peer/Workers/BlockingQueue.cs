using System.Collections.Generic;
using System.Threading;

namespace ADL.Node.Core.Modules.Peer.Workers
{
    class BlockingQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _queueLock = new object();
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public T Take()
        {
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                    return _queue.Dequeue();
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

        public int Count
        {
            get
            {
                return _queue.Count;
            }
        }
    }
}