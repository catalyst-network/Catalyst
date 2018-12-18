using System.Net;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerIdentifier
    {
        internal byte[] PublicKey { get; set; }
        internal IPEndPoint EndPoint { get; set; }   
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
