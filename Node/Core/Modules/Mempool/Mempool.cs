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
        private IKeyStore KeyStore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="keyStore"></param>
        public Mempool(IKeyStore keyStore)
        {
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
            return KeyStore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        Tx IMempool.GetTx(Key k)
        {
            return GetTx(k);
        }

        /// <summary>
        /// 
        /// </summary>
        /// 
        /// <param name="k"></param>
        /// <returns></returns>
        public Tx GetTx(Key k)
        {
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
 