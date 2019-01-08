using System;
using ADL.Util;
using System.Net;
using ADL.Network;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Peer
{
    public abstract class Peer : IDisposable
    {   
        private int Reputation { get; set; }
        private bool Disposed  { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Connection Connection { get; set; }
        public PeerIdentifier PeerIdentifier { get; }
        public bool IsAwolBot => InactiveFor > TimeSpan.FromMinutes(30);
        private TimeSpan InactiveFor => DateTimeProvider.UtcNow - LastSeen;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <param name="endpoint"></param>
        /// <exception cref="ArgumentException"></exception>
        private Peer(PeerIdentifier peerIdentifier, IPEndPoint endpoint)
        {
            if (peerIdentifier == null) throw new ArgumentNullException(nameof(peerIdentifier));
            if (!Ip.ValidPortRange(EndPoint.Port)) throw new ArgumentException("Peer Endpoint port range invalid");
            
            EndPoint = endpoint;
            PeerIdentifier = peerIdentifier;
            LastSeen = DateTimeProvider.UtcNow;
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal void Touch()
        {
            LastSeen = DateTimeProvider.UtcNow;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void IncreaseReputation()
        {
            Reputation++;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void DecreaseReputation()
        {
            //@TODO check if this is bellow ban threshold
            Reputation--;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            Log.Log.Message("disposing peer class");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
//                Connection.Dispose();
            }
            
            Disposed = true;    
            Log.Log.Message($"Peer {PeerIdentifier.Id} disposed");
        }
    }
}
