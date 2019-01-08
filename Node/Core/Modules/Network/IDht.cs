
using System.Collections.Generic;
using ADL.Node.Core.Modules.Network.Peer;

namespace ADL.Node.Core.Modules.Network
{
    public interface IDht
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool Ping();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        bool Store(string k, byte[] v);
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<PeerIdentifier> FindNode();
    }
}
