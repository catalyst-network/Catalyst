using System;
using ADL.Util;
using System.Net;
using ADL.Network;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Peer
{
    public abstract class Peer : IDisposable
    {   
        private bool Disposed  { get; set; }
        public bool Connected { get; set; }
        public DateTime LastSeen { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Connection Connection { get; set; }
        public int Reputation { get; private set; }
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
            if (!Ip.ValidPortRange(EndPoint.Port)) throw new ArgumentException("Peer Endpoint port range invalid");
            
            EndPoint = endpoint;
            PeerIdentifier = peerIdentifier;
            LastSeen = DateTimeProvider.UtcNow;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public void AddConnection(Connection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Connected = true;
        }
        
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
        
        public void Dispose()
        {
            Dispose(true);
            Log.Log.Message("disposing peer class");
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                Connection.Dispose();
            }
            
            Disposed = true;    
            Log.Log.Message("Peer class disposed");
        }
    }
}
