using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// 
    /// </summary>
    public class PeerIdentifier
    { 
        public long Nonce { set; get; }
        public byte[] NodeId { set; get; }
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
            NodeId = GenerateNodeId();
        }

        /// <summary>
        /// Node ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
        /// the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4 the leading 12 bytes should all be 0x0 
        /// clientID [2] + clientVersion[4] + Ip[16] + Port[2] + random[18]
        /// The client ID for this implementation is "AC" or hexadecimal 4143
        /// </summary>
        /// <returns></returns>
        private byte[] GenerateNodeId()
        {
            byte[] nodeId = new byte[42];

            byte[] clientIdChunk = Encoding.ASCII.GetBytes("AC");
            
            Buffer.BlockCopy(clientIdChunk, 0, nodeId, 0, 2);
            
            string[] versionString = "1.12.24".Split('.'); //@TODO get assembly version
            
            for (int i = 0; i < versionString.Length; i++)
            {
                if (versionString[i].Length < 2)
                {
                    versionString[i] = versionString[i].PadLeft(2, '0');
                }
            }
                            
            var versionBytes = Encoding.ASCII.GetBytes(string.Join("", versionString).Substring(0, 4));
            Buffer.BlockCopy(versionBytes, 0, nodeId, 2, 2);
            byte[] ipChunk = new byte[16];
            IPAddress address = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
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
            
            Buffer.BlockCopy(ipChunk, 0, nodeId, 4, 16);
            int port = 65535;
            byte[] portBytes = Encoding.ASCII.GetBytes(port.ToString("X"));
            byte[] portChunk = new byte[2];
            if (portBytes.Length > 4)
            {
                
            }
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(port.ToString("X")), 0, nodeId, 20, 2);
      
            var returnArray = new byte[20];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(returnArray);
            Buffer.BlockCopy(returnArray, 0, nodeId, 22, 20);
            
            Console.WriteLine(BitConverter.ToString(nodeId));
        }
    }
}
