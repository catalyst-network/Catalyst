using System;
using Lib.P2P;

namespace Catalyst.Abstractions.Dfs.BlockExchange
{
    /// <summary>
    ///   The content addressable ID related to an event. 
    /// </summary>
    /// <see cref="Cid"/>
    /// <see cref="Catalyst.Core.Modules.Dfs.BlockExchange.BitswapService.BlockNeeded"/>
    public class CidEventArgs : EventArgs
    {
        /// <summary>
        ///   The content addressable ID. 
        /// </summary>
        /// <value>
        ///   The unique ID of the block.
        /// </value>
        public Cid Id { get; set; }
    }
}
