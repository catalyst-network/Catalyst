using System.Collections.Generic;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   The statistics for <see cref="IStatsApi.BitSwapAsync"/>.
    /// </summary>
    public class BitswapData
    {
        /// <summary>
        ///   TODO: Unknown.
        /// </summary>
        public int ProvideBufLen;

        /// <summary>
        ///   The content that is wanted.
        /// </summary>
        public IEnumerable<Cid> Wantlist;

        /// <summary>
        ///   The known peers.
        /// </summary>
        public IEnumerable<MultiHash> Peers;

        /// <summary>
        ///   The number of blocks sent by other peers.
        /// </summary>
        public ulong BlocksReceived;

        /// <summary>
        ///   The number of bytes sent by other peers.
        /// </summary>
        public ulong DataReceived;

        /// <summary>
        ///   The number of blocks sent to other peers.
        /// </summary>
        public ulong BlocksSent;

        /// <summary>
        ///   The number of bytes sent to other peers.
        /// </summary>
        public ulong DataSent;

        /// <summary>
        ///   The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupBlksReceived;

        /// <summary>
        ///   The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        public ulong DupDataReceived;
    }
}
