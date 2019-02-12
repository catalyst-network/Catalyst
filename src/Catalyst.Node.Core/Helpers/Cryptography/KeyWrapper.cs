using System.Data.SqlTypes;
using Catalyst.Protocols.Mempool;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public abstract class KeyWrapper<T>
    {
        private T _key;
        protected T Key
        {
            get { return _key; }
            private set { _key = value;
                this.Empty = false;
            }
        } 
        private bool _isEmpty = true;
        public bool Empty
        {
            get { return _isEmpty;}
            private set { _isEmpty = value; }
        }
        protected KeyWrapper(T key)
        {
            this.Key = key;
        }
        
        protected KeyWrapper(){}
    }
}