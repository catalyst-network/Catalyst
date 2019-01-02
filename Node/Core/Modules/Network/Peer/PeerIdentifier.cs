using System;
using ADL.Util;
using System.Net;
using System.Text;
using ADL.Network;
using System.Reflection;
using System.Text.RegularExpressions;
using ADL.Hex.HexConvertors.Extensions;
using ADL.RLP;

namespace ADL.Node.Core.Modules.Network.Peer
{
    /// <summary>
    /// Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
    /// the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4 the leading 12 bytes should be padded 0x0 
    /// clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
    /// The client ID for this implementation is "AC" or hexadecimal 4143
    /// </summary>
    public class PeerIdentifier
    { 
        public byte[] Id { set; get; }
        public bool Known { get; set; }        
        
        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public PeerIdentifier(byte[] id)
        {
            if (!ValidatePeerId(id))
            {
                throw new ArgumentException("Peer identifier is invalid.");
            }
            
            Id = id;
            Known = false;
        }
        
        /// <summary>
        /// method to build our peerId
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static PeerIdentifier BuildPeerId(byte[] publicKey, IPEndPoint endPoint)
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

            return new PeerIdentifier(peerId);
        }

        /// <summary>
        /// Get hex of this client
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientIdChunk()
        {
            return Encoding.UTF8.GetBytes("AC");
        }

        /// <summary>
        /// We only care about the major ass string! 🍑 🍑 🍑 
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientVersionChunk()
        {
            return Encoding.ASCII.GetBytes(PadVersionString(Assembly.GetExecutingAssembly().GetName().Version.Major.ToString()));
        }

        private static string PadVersionString(string version)
        {
            while (version.Length < 2)
            {
                version = version.PadLeft(2, '0');
            }

            return version;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientIpChunk(IPEndPoint endPoint)
        {
            byte[] ipChunk = new byte[16];
            IPAddress address = Ip.GetPublicIP();
            byte[] ipBytes = address.GetAddressBytes();
            
            if (ipBytes.Length == 4)
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
            return endPoint.Port.ToBytesForRLPEncoding();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool ValidatePeerId(byte[] peerId)
        {
            if (peerId == null) throw new ArgumentNullException(nameof(peerId));

            try
            {
                ValidatePeerIdLength(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidatePeerIdLength", e);
                return false;
            }
            
            try
            {
                ValidateClientId(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidateClientId", e);
                return false;
            }
            
            try
            {
                ValidateClientVersion(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidateClientVersion", e);
                return false;
            }
            
            try
            {
                ValidateClientIp(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidateClientIp", e);
                return false;
            }
            
            try
            {
                ValidateClientPort(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidateClientPort", e);
                return false;
            }
            
            try
            {
                ValidateClientPubKey(peerId);
            }
            catch (ArgumentException e)
            {
                Log.LogException.Message("ValidateClientPubKey", e);
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidatePeerIdLength(byte[] peerId)
        {
            if (peerId.Length != 42)
            {
                throw new ArgumentException("peerId must be 42 bytes");
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientId(byte[] peerId)
        {
            if (!Regex.IsMatch(ByteUtil.ByteToString(peerId.Slice(0, 2)), @"^[a-zA-Z]+$"))
            {
                throw new ArgumentException("ClientID not valid");
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientVersion(byte[] peerId)
        {            
            if (!peerId.Slice(2, 4).ToHex().IsTheSameHex(PadVersionString(Assembly.GetExecutingAssembly().GetName().Version.Major.ToString()).ToHexUTF8()))
            {
                throw new ArgumentException("clientVersion not valid");
                //@TODO we need to discuss how major version updates will be rolled out as we could potentially partition the network here!!!!
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientIp(byte[] peerId)
        {
            if (Ip.ValidateIp(new IPAddress(peerId.Slice(4, 20)).ToString()).GetType() != typeof(IPAddress))
            {
                //@TODO we need to validate the proclaimed ip in the identifier is the actual same ip from the client endpoint.
                throw new ArgumentException("clientIp not valid"); 
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientPort(byte[] peerId)
        {
            if (!Ip.ValidPortRange( peerId.Slice(20, 22).ToIntFromRLPDecoded()))
            {
                throw new ArgumentException("clientPort not valid"); 
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientPubKey(byte[] peerId)
        {
            if (peerId.Slice(22, 42).Length != 20)
            {
                //@TODO this feels stupid as you define the length as 20 while calling the Slice method, should hook into a global pubkey validation method but we need to define a valid address format
                throw new ArgumentException("clientPubKey not valid"); 
            }
        }
    }
}
