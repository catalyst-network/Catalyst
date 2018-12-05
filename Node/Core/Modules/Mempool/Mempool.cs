using System;
using ADL.DataStore;
using ADL.Protocol.Mempool.dist.csharp;
using Google.Protobuf;

namespace ADL.Node.Core.Modules.Mempool
{
    public class Mempool : IMempool
    {
        private IKeyStore KeySore;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="keyStore"></param>
        public Mempool(IKeyStore keyStore)
        {
            KeySore = keyStore;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SaveTx(Key k, Tx value)
        {
            return KeySore.Set(k.ToByteArray(), value.ToByteArray(), null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        public Tx GetTx(Key k)
        {
            return Tx.Parser.ParseFrom(KeySore.Get(k.ToByteArray()));
        }
    }
}
 