using System.Collections.Generic;

namespace Catalyst.Node.Common.Modules
{
    public interface IP2P
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool Ping(IPeerIdentifier queryingNode);

        /// <summary>
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        List<IPeerIdentifier> FindNode(IPeerIdentifier queryingNode, IPeerIdentifier targetNode);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        List<IPeerIdentifier> GetPeers(IPeerIdentifier queryingNode);

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        bool Store(string k, byte[] v);

        /// <summary>
        ///     If a corresponding value is present on the queried node, the associated data is returned.
        ///     Otherwise the return value is the return equivalent to FindNode()
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        dynamic FindValue(string k);

        /// <summary>
        ///     Reflects back current nodes peer bucket
        /// </summary>
        /// <param name="queryingNode"></param>
        /// <returns></returns>
        List<IPeerIdentifier> PeerExchange(IPeerIdentifier queryingNode);
    }
}