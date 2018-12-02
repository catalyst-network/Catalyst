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
        private IPeerSettings PeerSettings { get; set; }

        public PeerService(IPeer peer, IPeerSettings peerSettings, NodeOptions options)
        {
            Peer = peer;
            PeerSettings = peerSettings;
            DataDir = options.DataDir;
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
            await Peer.StartPeer();
        }

            
        public bool StopService()
        {
            return Peer.StopPeer();
        }
    }
}
