using System;
using ADL.Node.Core.Modules.Network.Peer;

namespace ADL.Node.Core.Modules.Network.Events
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
            if (nodeIdentity == null) throw new ArgumentNullException(nameof (nodeIdentity));
            Log.Log.Message($"AnnounceNodeEventArgs {nodeIdentity.Id}");
            NodeIdentity = nodeIdentity;
        }
    }
}
