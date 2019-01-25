using System;
using System.Collections.Generic;
using Catalyst.Helpers.KeyValueStore;
using Catalyst.Helpers.Util;
using Catalyst.Protocols.Mempool;
using Google.Protobuf;

namespace Catalyst.Node.Modules.Core.Mempool
{
    /// <summary>
    ///     Mempool class wraps around a IKeyValueStore
    /// </summary>
    public class Mempool : IMempool, IDisposable
    {
        private static Mempool Instance { get; set; }
        private static readonly object Mutex = new object();
        
        public IKeyValueStore _keyValueStore { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="keyValueStore"></param>
        private Mempool(IKeyValueStore keyValueStore)
        {
            Guard.NotNull(keyValueStore, nameof(keyValueStore));
            _keyValueStore = keyValueStore;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        /// <returns></returns>
        public static Mempool GetInstance(IKeyValueStore keyValueStore)
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Mempool(keyValueStore);
                }
            return Instance;
        }
        
        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SaveTx(Key k, Tx value)
        {
            Guard.NotNull(k, nameof(k));
            Guard.NotNull(value, nameof(value));
            return _keyValueStore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Tx GetTx(Key k)
        {
            Guard.NotNull(k, nameof(k));
            return Tx.Parser.ParseFrom(_keyValueStore.Get(k.ToByteArray()));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetMempool()
        {
            return _keyValueStore.GetInfo();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }
    }
}