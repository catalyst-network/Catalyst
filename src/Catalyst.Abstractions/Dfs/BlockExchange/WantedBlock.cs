using System.Collections.Generic;
using System.Threading.Tasks;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.BlockExchange
{
    /// <summary>
    ///   A content addressable block that is wanted by a peer.
    /// </summary>
    public class WantedBlock
    {
        /// <summary>
        ///   The content ID of the block;
        /// </summary>
        public Cid Id;

        /// <summary>
        ///   The peers that want the block.
        /// </summary>
        public List<MultiHash> Peers;

        /// <summary>
        ///   The consumers that are waiting for the block.
        /// </summary>
        public List<TaskCompletionSource<IDataBlock>> Consumers;
    }
}
