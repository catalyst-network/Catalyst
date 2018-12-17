using System.Net;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerIdentifier
    {
        private static IPEndPoint EndPoint { get; set; }   
        private static byte[] PublicKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        public PeerIdentifier(byte[] publicKey, IPEndPoint endPoint)
        {
            PublicKey = publicKey;
            EndPoint = endPoint;
        }
    }
}
