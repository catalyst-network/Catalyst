using System;
using System.IO;
using System.Threading.Tasks;
using ADL.Cryptography.SSL;

namespace ADL.P2P
{
    /// <summary>
    /// The P2P Service 
    /// </summary>
    public class P2PService : IP2P
    {
        /// <summary>
        /// P2PController constructor.
        /// </summary>
        public P2PService()
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
        public Task StartServer(IP2PSettings p2PSettings, ISslSettings sslSettings, DirectoryInfo dataDir)
        {
            return Peer.StartService(p2PSettings, sslSettings, dataDir);
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
