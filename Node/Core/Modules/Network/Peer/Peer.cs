using System;
using System.Net;
using ADL.Util;

namespace ADL.Node.Core.Modules.Network.Peer
{
    public class Peer
    {   
        private Peer(PeerIdentifier peerIdentifier, IPEndPoint endpoint)
        {
            PeerIdentifier = peerIdentifier;
            EndPoint = endpoint;
            LastSeen = DateTimeProvider.UtcNow;
        }
        
        public PeerIdentifier PeerIdentifier { get; }

        public byte[] PublicKey { get; set; }
        public short NodeVersion { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public int Reputation { get; private set; }
        public bool IsUnknownNode => Reputation < -10;
        public bool IsAWOLBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;
        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }

        public void DecreaseReputation()
        {
            Reputation--;
        }
    }
}