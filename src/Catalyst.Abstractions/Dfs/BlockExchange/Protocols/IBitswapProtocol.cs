using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Protocols;

namespace Catalyst.Abstractions.Dfs.BlockExchange.Protocols
{
    /// <summary>
    ///   Features of a bitswap protocol.
    /// </summary>
    public interface IBitswapProtocol : IPeerProtocol
    {
        /// <summary>
        ///   Send a want list.
        /// </summary>
        /// <param name="stream">
        ///   The destination of the want list.
        /// </param>
        /// <param name="wants">
        ///   A sequence of <see cref="Catalyst.Core.Modules.Dfs.BlockExchange.WantedBlock"/>.
        /// </param>
        /// <param name="full">
        ///   <b>true</b> if <paramref name="wants"/> is the full want list.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task SendWantsAsync(Stream stream,
            IEnumerable<WantedBlock> wants,
            bool full = true,
            CancellationToken cancel = default(CancellationToken));
    }
}
