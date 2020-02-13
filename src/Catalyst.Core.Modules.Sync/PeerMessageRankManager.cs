using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Catalyst.Core.Modules.Sync
{
    public abstract class PeerMessageRankManager<TKey, TValue>
    {
        protected IDictionary<TKey, TValue> _messages = new ConcurrentDictionary<TKey, TValue>();
        public virtual void Add(TKey key, TValue value)
        {
             _messages[key] = value;
        }
    }
}
