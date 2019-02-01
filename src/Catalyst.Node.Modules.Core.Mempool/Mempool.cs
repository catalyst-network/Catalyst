using System;
using System.Collections.Generic;
using Catalyst.Helpers.KeyValueStore;
using Catalyst.Helpers.Util;
using Catalyst.Protocols.Mempool;
using Dawn;
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
        public Mempool(IKeyValueStore keyValueStore)
        {
            Guard.Argument(keyValueStore, nameof(keyValueStore)).NotNull();
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
            Guard.Argument(k, nameof(k)).NotNull();
            Guard.Argument(value, nameof(value)).NotNull();
            return _keyValueStore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Tx GetTx(Key k)
        {
            Guard.Argument(k, nameof(k)).NotNull();
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