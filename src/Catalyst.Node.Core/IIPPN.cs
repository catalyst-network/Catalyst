using System.Collections.Generic;
using Catalyst.Node.Core.Modules.P2P;

namespace Catalyst.Node.Core
{
    public interface IIPPN
    {
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool Ping(PeerIdentifier queryingNode);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        List<PeerIdentifier> FindNode(PeerIdentifier queryingNode, PeerIdentifier targetNode);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<PeerIdentifier> GetPeers(PeerIdentifier queryingNode);

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        bool Store(string k, byte[] v);

        /// <summary>
        ///  If a corresponding value is present on the queried node, the associated data is returned.
        ///  Otherwise the return value is the return equivalent to FindNode()
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        dynamic FindValue(string k);
        
        /// <summary>
        /// Reflects back current nodes peer bucket
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <returns></returns>
        List<PeerIdentifier> PeerExchange(PeerIdentifier queryingNode);
    }
}