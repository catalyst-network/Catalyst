#region LICENSE
/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
* 
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    ///     Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
    ///     the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4
    ///     the leading 12 bytes should be padded 0x0
    ///     clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
    ///     The client ID for this implementation is "AC" or hexadecimal 4143
    /// </summary>
    public class PeerIdentifier : IPeerIdentifier
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        private PeerIdentifier(byte[] id)
        {
            if (!ValidatePeerId(id))
            {
                throw new ArgumentException("Peer identifier is invalid.");
            }

            Id = id;
        }

        /// <summary>
        ///     method to build our peerId
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public PeerIdentifier(byte[] publicKey, IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            Logger.Information(publicKey.Length.ToString());
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

            if (!ValidatePeerId(peerId))
            {
                throw new ArgumentException("Peer identifier is invalid.");
            }

            Id = peerId;
        }

        public byte[] Id { get; }

        /// <summary>
        ///     Get hex of this client
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientIdChunk() { return Encoding.UTF8.GetBytes("AC"); }

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
            while (version.Length < 2)
            {
                version = version.PadLeft(2, '0');
            }
            return version;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private static byte[] BuildClientIpChunk()
        {
            var ipChunk = new byte[16]; // @TODO hook into new byte method
            var address = Ip.GetPublicIpAsync().GetAwaiter().GetResult();
            var ipBytes = address.GetAddressBytes();

            if (ipBytes.Length == 4)
            {
                Buffer.BlockCopy(ipBytes, 0, ipChunk, 12, 4);
            }
            else
            {
                ipChunk = ipBytes;
            }

            Logger.Debug(string.Join(" ", ipChunk));

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
            var buildClientPortChunk = endPoint.Port.ToBytesForRLPEncoding();
            Logger.Debug(string.Join(" ", buildClientPortChunk));
            return buildClientPortChunk;
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private bool ValidatePeerId(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();

            try
            {
                ValidatePeerIdLength(peerId);
                ValidateClientId(peerId);
                ValidateClientVersion(peerId);
                ValidateClientIp(peerId);
                ValidateClientPort(peerId);
                ValidateClientPubKey(peerId);
            }
            catch (ArgumentException e)
            {
                Logger.Error(e, "Failed to validate Peer Id");
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidatePeerIdLength(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty()
               .MinCount(42)
               .MaxCount(42);
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientId(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();
            
            if (!Regex.IsMatch(ByteUtil.ByteToString(peerId.Slice(0, 2)), @"^[a-zA-Z]+$"))
            {
                throw new ArgumentException("ClientID not valid");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientVersion(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();
            
            if (!peerId.Slice(2, 4).ToHex()
               .IsTheSameHex(
                    PadVersionString(Assembly.GetExecutingAssembly().GetName().Version.Major.ToString())
                       .ToHexUTF8()))
            {
                throw new ArgumentException("clientVersion not valid");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateClientIp(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();
            
            if (Ip.ValidateIp(new IPAddress(peerId.Slice(4, 20)).ToString()).GetType() != typeof(IPAddress))
            {
                throw new ArgumentException("clientIp not valid");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateClientPort(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();
            
            if (!Ip.ValidPortRange(peerId.Slice(20, 22).ToIntFromRLPDecoded()))
            {
                throw new ArgumentException("clientPort not valid");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="peerId"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateClientPubKey(byte[] peerId)
        {
            Guard.Argument(peerId, nameof(peerId))
               .NotNull()
               .NotEmpty();
            
            if (peerId.Slice(22, 42).Length != 20)
            {
                throw new ArgumentException("clientPubKey not valid");
            }
        }
    }
}
