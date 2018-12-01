using System;
using System.IO;
using System.Threading.Tasks;

namespace ADL.Peer
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class PeerService : IPeer
    {
        /// <summary>
        /// PeerController constructor.
        /// </summary>
        public PeerService()
        {
            DateTime currentUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Returns a UTC DateTime obj
        /// </summary>
        /// <returns></returns>
        DateTime CurrentUTCTime()
        {
            return DateTime.UtcNow;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="p2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public Task StartServer(IPeerSettings peerSettings, DirectoryInfo dataDir)
        {
            return Peer.StartService(peerSettings, dataDir);
        }
            
        public bool StopServer()
        {
            return Peer.ShutDown();
        }

        void PingPeer()
        {
            
        }

        void GetPeerInfo()
        {
            
        }

        void GetPeerNeighbors()
        {
            
        }
    }
}
