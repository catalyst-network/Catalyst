using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Peer
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class PeerService : ServiceBase, IPeerService
    {
        private IPeer Peer { get; set; }
        private string DataDir { get; set; }
        private ISslSettings SslSettings { get; set; }
        private IPeerSettings PeerSettings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="peerSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="options"></param>
        public PeerService(IPeer peer, IPeerSettings peerSettings, ISslSettings sslSettings, NodeOptions options)
        {
            Peer = peer;
            SslSettings = sslSettings;
            DataDir = options.DataDir;
            PeerSettings = peerSettings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public async Task StartService()
        {
            await Peer.StartPeer(SslSettings, DataDir);
        }

            
        public bool StopService()
        {
            return Peer.StopPeer();
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IPeer GetImpl()
        {
            return Peer;
        }
    }
}
