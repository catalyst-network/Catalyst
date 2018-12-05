using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ADL.Cryptography;
using ADL.Cryptography.SSL;
using ADL.Node.Core.Helpers.Services;
using ADL.Protocol.Peer;
using Google.Protobuf;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

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
        private Client client { get; set; }

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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        bool MessageReceived(byte[] data)
        {
            var challenge = ADL.Protocol.Peer.ChallengeRequest.Parser.ParseFrom(data);
            Console.WriteLine("Message from server: " + BytesToHex(data) + challenge);
            var charllengeResponse = new ChallengeResponse();
            AsymmetricCipherKeyPair publicKeyPair = Ec.CreateKeyPair();
            charllengeResponse.Type = 10;
            Console.WriteLine(publicKeyPair.Public.ToString());
            charllengeResponse.PublicKey = publicKeyPair.Public.ToString();
            charllengeResponse.SignedNonce = Ec.SignData(challenge.Nonce.ToString(), publicKeyPair.Private);
            client.SendAsync(charllengeResponse.ToByteArray());

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
//            Network.GetInstance(PeerSettings, SslSettings, DataDir);
            
            client = new Client(
                "127.0.0.1",
                42069,
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
