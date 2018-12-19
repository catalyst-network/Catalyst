using System.Net;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerIdentifier
    { 
        public long Nonce { set; get; }
        public string ClientId { set; get; }
        public short NodeVersion { get; set; }
        private byte[] PublicKey { get; set; }
        private IPEndPoint EndPoint { get; set; }

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

        /// <summary>
        /// Node ID's should return a unsigned 32 byte array in the following format, to produce a 256 bit key space
        /// clientID [2] + clientVersion[4] + Ip[4] + Port[2] + publicKeyHash[20]
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateNodeId()
        {
            
        }
    }
}
