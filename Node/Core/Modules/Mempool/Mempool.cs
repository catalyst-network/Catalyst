using System;
using System.Collections.Generic;
using System.Linq;
using ADL.DataStore;
using ADL.Protocols.Mempool;
using Google.Protobuf;

namespace ADL.Node.Core.Modules.Mempool
{
    public class Mempool : IMempool
    {
        private readonly IKeyStore KeyStore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyStore"></param>
        public Mempool(IKeyStore keyStore)
        {
            if (keyStore == null) throw new ArgumentNullException(nameof(keyStore));
            KeyStore = keyStore;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SaveTx(Key k, Tx value)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            if (value == null) throw new ArgumentNullException(nameof(value));
            return KeyStore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Tx IMempool.GetTx(Key k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            return GetTx(k);
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="k"></param>
        /// <returns></returns>
        private Tx GetTx(Key k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            return Tx.Parser.ParseFrom(KeyStore.Get(k.ToByteArray()));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <returns></returns>
        public Dictionary<string, string> GetInfo()
        {
            return KeyStore.GetInfo();
        }
    }
}
 