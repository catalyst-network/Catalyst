using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerIdentifier
    { 
        public long Nonce { set; get; }
        public byte[] PeerId { set; get; }
        private byte[] PublicKey { get; set; }
        private IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        public PeerIdentifier(byte[] publicKey, IPEndPoint endPoint)
        {
            // we need a public key with at least 20 bytes anything else wont do.
            if (publicKey.Length < 20)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            PublicKey = publicKey;
            EndPoint = endPoint;
            PeerId = BuildPeerId();
        }

        /// <summary>
        /// Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
        /// the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4 the leading 12 bytes should be padded 0x0 
        /// clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
        /// The client ID for this implementation is "AC" or hexadecimal 4143
        /// </summary>
        /// <returns></returns>
        private byte[] BuildPeerId()
        {
            // init blank nodeId
            byte[] peerId = new byte[42];
            
            // copy client id chunk
            Buffer.BlockCopy(BuildClientIdChunk(), 0, peerId, 0, 2);
            
            // copy client version chunk
            Buffer.BlockCopy(BuildClientVersionChunk(), 0, peerId, 2, 2);
            
            // copy client ip chunk
            Buffer.BlockCopy(BuildClientIpChunk(), 0, peerId, 4, 16);

            // copy client port chunk
            Buffer.BlockCopy(BuildClientPortChunk(), 0, peerId, 20, 2);
      
            // copy client public key chunk
            Buffer.BlockCopy(PublicKey, 0, peerId, 22, 20);
            
            // log if we are debugging
            Log.Log.Message(BitConverter.ToString(peerId));
            
            return peerId;
        }

        private byte[] BuildClientIdChunk()
        {
            return Encoding.ASCII.GetBytes("AC");
        }

        /// <summary>
        /// We only care about the major ass string! 🍑 🍑 🍑 
        /// </summary>
        /// <returns></returns>
        private byte[] BuildClientVersionChunk()
        {
            Version assVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string majorAssString = assVersion.Major.ToString();
            
            if (majorAssString.Length < 2)
            {
                majorAssString = majorAssString.PadLeft(2, '0');
            }
                            
            return Encoding.ASCII.GetBytes(majorAssString);
        }

        private byte[] BuildClientIpChunk()
        {
            byte[] ipChunk = new byte[16];
            IPAddress address = IPAddress.Parse(ADL.Network.Ip.GetPublicIP());
            byte[] ipBytes = address.GetAddressBytes();
            
            if(ipBytes.Length == 4)
            {
                Buffer.BlockCopy(ipBytes, 0, ipChunk, 12, 4);
                Console.WriteLine(BitConverter.ToString(ipChunk));
            }
            else
            {
                ipChunk = ipBytes;
            }

            return ipChunk;
        }

        private byte[] BuildClientPortChunk()
        {
            int port = 65535; //@TODO get this from our bind endpoint
            byte[] portBytes = Encoding.ASCII.GetBytes(port.ToString("X"));
            byte[] portChunk = new byte[2];
            if (portBytes.Length > 4)
            {
               //@TODO pad with 0x0 so we always have same length.
            }
            return Encoding.ASCII.GetBytes(port.ToString("X"));
        }
    }
}
