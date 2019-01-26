using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Catalyst.Helpers.Hex.HexConverters.Extensions;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Network;
using Catalyst.Helpers.RLP;
using Catalyst.Helpers.Util;
using Dawn;

namespace Catalyst.Node.Modules.Core.P2P.Peer
{
    /// <summary>
    ///     Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
    ///     the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4
    ///     the leading 12 bytes should be padded 0x0
    ///     clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
    ///     The client ID for this implementation is "AC" or hexadecimal 4143
    /// </summary>
    public class PeerIdentifier
    {
        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public PeerIdentifier(byte[] id)
        {
            if (!ValidatePeerId(id)) throw new ArgumentException("Peer identifier is invalid.");

            Id = id;
        }

        public byte[] Id { set; get; }

        /// <summary>
        ///     method to build our peerId
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static PeerIdentifier BuildPeerId(byte[] publicKey, IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            Log.Message(publicKey.Length.ToString());
            Guard.Argument(publicKey, nameof(publicKey)).NotNull().NotEmpty().MaxCount(20).MinCount(20);

            // init blank nodeId
            var peerId = new byte[42]; //@TODO hook into new byte method

            // copy client id chunk
            Buffer.BlockCopy(BuildClientIdChunk(), 0, peerId, 0, 2);

            // copy client version chunk
            Buffer.BlockCopy(BuildClientVersionChunk(), 0, peerId, 2, 2);

            // copy client ip chunk
            Buffer.BlockCopy(BuildClientIpChunk(), 0, peerId, 4, 16);

            // copy client port chunk
            Buffer.BlockCopy(BuildClientPortChunk(endPoint), 0, peerId, 20, 2);

            // copy client public key chunk
            Buffer.BlockCopy(publicKey, 0, peerId, 22, 20);

            return new PeerIdentifier(peerId);
        }

        /// <summary>
        ///     Get hex of this client
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientIdChunk()
        {
            return Encoding.UTF8.GetBytes("AC");
        }

        /// <summary>
        ///     We only care about the major ass string! 🍑 🍑 🍑
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientVersionChunk()
        {
            return Encoding.UTF8.GetBytes(
                PadVersionString(Assembly.GetExecutingAssembly().GetName().Version.Major.ToString()));
        }

        /// <summary>
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private static string PadVersionString(string version)
        {
            Guard.Argument(version, nameof(version)).NotNull().NotEmpty().NotWhiteSpace();
            while (version.Length < 2) version = version.PadLeft(2, '0');
            return version;
        }

        /// <summary>
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientIpChunk()
        {
            var ipChunk = new byte[16]; // @TODO hook into new byte method
            var address = Ip.GetPublicIp();
            var ipBytes = address.GetAddressBytes();

            if (ipBytes.Length == 4)
                Buffer.BlockCopy(ipBytes, 0, ipChunk, 12, 4); //@TODO hook into byte util
            else
                ipChunk = ipBytes;
            Log.ByteArr(ipBytes);

            return ipChunk;
        }

        /// <summary>
        ///     @TODO this gets the connection end point for our port rather than the advertised port
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientPortChunk(IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            Log.ByteArr(endPoint.Port.ToBytesForRlpEncoding());
            return endPoint.Port.ToBytesForRlpEncoding();
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool ValidatePeerId(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
                .NotNull()
                .NotEmpty()
                .MinCount(42)
                .MaxCount(42);
            
            if (peerId == null) throw new ArgumentNullException(nameof(peerId));

            try
            {
                ValidatePeerIdLength(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidatePeerIdLength", e);
                return false;
            }

            try
            {
                ValidateClientId(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidateClientId", e);
                return false;
            }

            try
            {
                ValidateClientVersion(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidateClientVersion", e);
                return false;
            }

            try
            {
                ValidateClientIp(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidateClientIp", e);
                return false;
            }

            try
            {
                ValidateClientPort(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidateClientPort", e);
                return false;
            }

            try
            {
                ValidateClientPubKey(peerId);
            }
            catch (ArgumentException e)
            {
                LogException.Message("ValidateClientPubKey", e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidatePeerIdLength(byte[] peerId)
        {
            if (peerId.Length != 42) throw new ArgumentException("peerId must be 42 bytes");
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientId(byte[] peerId)
        {
            //@TODO have a known list of clients.
            if (!Regex.IsMatch(ByteUtil.ByteToString(peerId.Slice(0, 2)), @"^[a-zA-Z]+$"))
                throw new ArgumentException("ClientID not valid");
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientVersion(byte[] peerId)
        {
            if (!peerId.Slice(2, 4).ToHex()
                .IsTheSameHex(PadVersionString(Assembly.GetExecutingAssembly().GetName().Version.Major.ToString())
                    .ToHexUtf8())) throw new ArgumentException("clientVersion not valid");
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientIp(byte[] peerId)
        {
            if (Ip.ValidateIp(new IPAddress(peerId.Slice(4, 20)).ToString()).GetType() != typeof(IPAddress))
                throw new ArgumentException("clientIp not valid");
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientPort(byte[] peerId)
        {
            if (!Ip.ValidPortRange(peerId.Slice(20, 22).ToIntFromRlpDecoded()))
                throw new ArgumentException("clientPort not valid");
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientPubKey(byte[] peerId)
        {
            if (peerId.Slice(22, 42).Length != 20) throw new ArgumentException("clientPubKey not valid");
        }
    }
}