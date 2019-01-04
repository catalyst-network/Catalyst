using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Network
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class NetworkService : AsyncServiceBase, INetworkService
    {
        public Network Network { get; set; }
        private string DataDir { get; set; }
        private string PublicKey { get; set; }
        private ISslSettings SslSettings { get; set; }
        private INetworkSettings NetworkSettings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="networkSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="options"></param>
        public NetworkService(INetworkSettings networkSettings, ISslSettings sslSettings, NodeOptions options)
        {
            SslSettings = sslSettings;
            DataDir = options.DataDir;
            PublicKey = options.PublicKey;
            NetworkSettings = networkSettings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            Network = Network.GetInstance(NetworkSettings, SslSettings, DataDir, PublicKey);
//            Network.PeerManager.BuildOutBoundConnection("127.0.0.1", 42069);
            return true;
        }
            
        public override bool StopService()
        {
            Network.Dispose();
            return false;
        }
    }
} 
