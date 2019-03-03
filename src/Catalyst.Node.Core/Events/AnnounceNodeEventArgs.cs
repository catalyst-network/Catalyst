using System;
using Catalyst.Node.Common;
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
        public AnnounceNodeEventArgs(IPeerIdentifier nodeIdentity)
        {
            Guard.Argument(nodeIdentity, nameof(nodeIdentity)).NotNull();
            NodeIdentity = nodeIdentity;
        }

        private IPeerIdentifier NodeIdentity { get; }
    }
}