using System;
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
        private PeerBuilder PeerBuilder { get; set; }

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

        private static string BytesToHex(byte[] data)
        {
            if (data == null || data.Length < 1)
            {
                return "(null)";
            }

            return BitConverter.ToString(data).Replace("-", "");
        }
        
//        bool MessageReceived(byte[] data)
//        {
//            var challenge = ADL.Protocol.Peer.ChallengeRequest.Parser.ParseFrom(data);
//            Console.WriteLine("Message from server: " + BytesToHex(data) + challenge);
//            var charllengeResponse = new ChallengeResponse();
//            AsymmetricCipherKeyPair publicKeyPair = Ec.CreateKeyPair();
//            charllengeResponse.Type = 10;
//            Console.WriteLine(publicKeyPair.Public.ToString());
//            charllengeResponse.PublicKey = publicKeyPair.Public.ToString();
//            charllengeResponse.SignedNonce = Ec.SignData(challenge.Nonce.ToString(), publicKeyPair.Private);
//            PeerBuilder.SendAsync(charllengeResponse.ToByteArray());
//
//            return true;
//        }
        
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
            
            PeerBuilder = new PeerBuilder(
                "127.0.0.1",
                42069,
                DataDir + "/" + SslSettings.PfxFileName,
               SslSettings.SslCertPassword,
                true,
                false,
                true);
                        
            return true;
        }
            
        public override bool StopService()
        {
//            return Peer.();
            return false;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IPeer GetImpl()
        {
            throw new NotImplementedException();
        }
    }
}
