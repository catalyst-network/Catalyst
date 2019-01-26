using System;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Peer;
using Dawn;

namespace Catalyst.Node.Events
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
            Log.Message($"AnnounceNodeEventArgs {nodeIdentity.Id}");
            NodeIdentity = nodeIdentity;
        }

        private PeerIdentifier NodeIdentity { get; }
    }
}