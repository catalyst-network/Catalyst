using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Peer
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class PeerService : AsyncServiceBase, IPeerService
    {
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
        public PeerService(IPeerSettings peerSettings, ISslSettings sslSettings, NodeOptions options)
        {
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
        public override bool StartService()
        {
             StartPeerProtocol();
             return true;
        }

        private async Task StartPeerProtocol()
        {
            await Peer.StartPeer(PeerSettings, SslSettings, DataDir);
        }
            
        public override bool StopService()
        {
            return Peer.StopPeer();
        }
    }
}
