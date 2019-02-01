using System;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Modules.P2P;
using Dawn;

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
            Log.Message($"AnnounceNodeEventArgs {nodeIdentity.Id}");
            NodeIdentity = nodeIdentity;
        }

        private PeerIdentifier NodeIdentity { get; }
    }
}