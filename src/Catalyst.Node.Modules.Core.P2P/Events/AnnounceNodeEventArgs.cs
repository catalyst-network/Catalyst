using System;
using Catalyst.Helpers.Logger;
using Catalyst.Node.Modules.Core.P2P.Peer;

namespace Catalyst.Node.Modules.Core.P2P.Events
{
    public class AnnounceNodeEventArgs : EventArgs
    {
        private PeerIdentifier NodeIdentity { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="nodeIdentity"></param>
        /// <exception cref="T:System.ArgumentNullException"></exception>
        public AnnounceNodeEventArgs(PeerIdentifier nodeIdentity)
        {
            //@TODO guard util
            if (nodeIdentity == null) throw new ArgumentNullException(nameof (nodeIdentity));
            Log.Message($"AnnounceNodeEventArgs {nodeIdentity.Id}");
            NodeIdentity = nodeIdentity;
        }
    }
}
