using System;
using System.Net;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Network;
using Catalyst.Helpers.Util;
using Catalyst.Helpers.IO;

namespace Catalyst.Node.Modules.Core.P2P.Peer
{
    public abstract class Peer : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <param name="endpoint"></param>
        /// <exception cref="ArgumentException"></exception>
        private Peer(PeerIdentifier peerIdentifier, IPEndPoint endpoint)
        {
            Guard.NotNull(peerIdentifier, nameof(peerIdentifier));
            if (!Ip.ValidPortRange(EndPoint.Port)) throw new ArgumentException("Peer Endpoint port range invalid");

            EndPoint = endpoint;
            PeerIdentifier = peerIdentifier;
            LastSeen = DateTimeProvider.UtcNow;
        }

        private int Reputation { get; set; }
        private bool Disposed { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Connection Connection { get; set; }
        public PeerIdentifier PeerIdentifier { get; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Log.Message("disposing peer class");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }

        /// <summary>
        /// </summary>
        public void IncreaseReputation()
        {
            Reputation++;
        }

        /// <summary>
        /// </summary>
        public void DecreaseReputation()
        {
            //@TODO check if this is bellow ban threshold
            Reputation--;
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
//                Connection.Dispose();
            }

            Disposed = true;
            Log.Message($"Peer {PeerIdentifier.Id} disposed");
        }
    }
}