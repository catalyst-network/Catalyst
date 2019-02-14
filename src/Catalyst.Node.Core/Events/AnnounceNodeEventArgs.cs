using System;
using Catalyst.Node.Core.P2P;
using Common.Logging;
using Dawn;
using Serilog;

namespace Catalyst.Node.Core.Events
{
    public class AnnounceNodeEventArgs : EventArgs
    {
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="nodeIdentity"></param>
        /// <exception cref="T:System.ArgumentNullException"></exception>
        public AnnounceNodeEventArgs(PeerIdentifier nodeIdentity)
        {
            Guard.Argument(nodeIdentity, nameof(nodeIdentity)).NotNull();
            NodeIdentity = nodeIdentity;
        }

        private PeerIdentifier NodeIdentity { get; }
    }
}