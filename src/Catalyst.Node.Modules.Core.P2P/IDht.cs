using System.Collections.Generic;
using Catalyst.Node.Modules.Core.P2P.Peer;

namespace Catalyst.Node.Modules.Core.P2P
{
    public interface IDht
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool Ping();

        /// <summary>
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        bool Store(string k, byte[] v);

        /// <summary>
        /// </summary>
        /// <returns></returns>
        List<IPeerIdentifier> FindNode();
    }
}