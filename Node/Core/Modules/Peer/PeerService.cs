using System;
using System.Text;
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
        

        static bool MessageReceived(byte[] data)
        {
            Console.WriteLine("Message from server: " + Encoding.UTF8.GetString(data));
            return true;
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
            Network.GetInstance(PeerSettings, SslSettings, DataDir);
            new Client(
                PeerSettings.BindAddress,
                PeerSettings.Port,
                DataDir + "/" + SslSettings.PfxFileName,
                SslSettings.SslCertPassword,
                true,
                false,
                () =>
                {
                    Console.WriteLine("client connected");
                    return true;
                },
                () =>
                {
                    Console.WriteLine("client disconnected");
                    return true;
                },
                MessageReceived,
                true);
            return true;
        }
            
        public override bool StopService()
        {
//            return Peer.();
            return false;
        }
    }
}
