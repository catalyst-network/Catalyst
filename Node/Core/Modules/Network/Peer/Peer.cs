using System;
using System.Net;
using ADL.Network;
using ADL.Util;

namespace ADL.Node.Core.Modules.Network.Peer
{
    public abstract class Peer
    {   
        private Peer(PeerIdentifier peerIdentifier, IPEndPoint endpoint)
        {
            if (!Ip.ValidPortRange(EndPoint.Port)) throw new ArgumentException("Peer Endpoint port range invalid");
            
            EndPoint = endpoint;
            PeerIdentifier = peerIdentifier;
            LastSeen = DateTimeProvider.UtcNow;
        }

        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public int Reputation { get; private set; }
        public PeerIdentifier PeerIdentifier { get; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;
        
        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }
        
        public void IncreaseReputation()
        {
            Reputation++;
        }
        
        public void DecreaseReputation()
        {
            Reputation--;
        }
        
    }
}