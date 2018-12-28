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
    public static class PeerIdentifier
    { 
        /// <summary>
        /// Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
        /// the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4 the leading 12 bytes should be padded 0x0 
        /// clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
        /// The client ID for this implementation is "AC" or hexadecimal 4143
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static byte[] BuildPeerId(byte[] publicKey, IPEndPoint endPoint)
        {
            // we need a public key with at least 20 bytes anything else wont do.
            if (publicKey.Length < 20)
            {
                throw new ArgumentException("public key must be 20 bytes long");
            }
            
            // init blank nodeId
            byte[] peerId = new byte[42];
            
            // copy client id chunk
            Buffer.BlockCopy(BuildClientIdChunk(), 0, peerId, 0, 2);
            
            // copy client version chunk
            Buffer.BlockCopy(BuildClientVersionChunk(), 0, peerId, 2, 2);
            
            // copy client ip chunk
            Buffer.BlockCopy(BuildClientIpChunk(endPoint), 0, peerId, 4, 16);

            // copy client port chunk
            Buffer.BlockCopy(BuildClientPortChunk(endPoint), 0, peerId, 20, 2);
      
            // copy client public key chunk
            Buffer.BlockCopy(publicKey, 0, peerId, 22, 20);
            
            Log.Log.Message(BitConverter.ToString(peerId));

            if (peerId.Length != 42)
            {
                throw new ArgumentException("peerId must be 42 bytes");
            }
            
            return peerId;
        }

        /// <summary>
        /// Get hex of this client
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientIdChunk()
        {
            return Encoding.ASCII.GetBytes("AC");
        }

        /// <summary>
        /// We only care about the major ass string! 🍑 🍑 🍑 
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientVersionChunk()
        {
            Version assVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string majorAssString = assVersion.Major.ToString();
            
            while (majorAssString.Length < 2)
            {
                majorAssString = majorAssString.PadLeft(2, '0');
            }
                            
            return Encoding.ASCII.GetBytes(majorAssString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientIpChunk(IPEndPoint endPoint)
        {
            byte[] ipChunk = new byte[16];
            IPAddress address = IPAddress.Parse(endPoint.Address.ToString());
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientPortChunk(IPEndPoint endPoint)
        {
            return Encoding.ASCII.GetBytes(endPoint.Port.ToString("X"));
        }
    }
}
