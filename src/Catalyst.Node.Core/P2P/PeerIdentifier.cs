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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
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

        public static readonly string AssemblyMajorVersion2Digits = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString("D2");
        public static readonly byte[] AssemblyMajorVersion2Bytes = Encoding.UTF8.GetBytes(AssemblyMajorVersion2Digits);

        public static readonly byte[] ClientId = Encoding.UTF8.GetBytes("AC");

        public byte[] Id => PeerId.ToByteArray();
        public PeerId PeerId { get; }

        public PeerIdentifier(PeerId peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).Require(ValidatePeerId);
            PeerId = peerId;
        }

        public PeerIdentifier(IPeerSettings settings) 
            : this(settings.PublicKey.HexToByteArray(), settings.EndPoint) {}

        public PeerIdentifier(byte[] publicKey, IPEndPoint endPoint)
        {
            PeerId = new PeerId()
            {
                PublicKey = publicKey.ToByteString(),
                Port = BuildClientPortChunk(endPoint).ToByteString(),
                Ip = endPoint.Address.To16Bytes().ToByteString(),
                ClientId = ClientId.ToByteString(),
                ClientVersion = AssemblyMajorVersion2Bytes.ToByteString()
            };
        }

        public static bool ValidatePeerId(PeerId peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).NotNull()
               .Require(p => p.PublicKey.Length == 20, _ => "PublicKey should be 20 bytes")
               .Require(p => p.Ip.Length == 16 && ValidateIp(p.Ip.ToByteArray()), _ => "Ip should be 16 bytes")
               .Require(p => ValidatePort(p.Port.ToByteArray()), _ => "Ip should be 16 bytes")
               .Require(p => ValidateClientId(p.ClientId.ToByteArray()),
                    _ => "ClientId should only be 2 alphabetical letters")
               .Require(p => ValidateClientVersion(p.ClientVersion.ToByteArray()), 
                    _ => $"ClientVersion doesn't match {AssemblyMajorVersion2Digits}");
            return true;
        }

        /// <summary>
        /// @TODO this gets the connection end point for our port rather than the advertised port
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        private static byte[] BuildClientPortChunk(IPEndPoint endPoint)
        {
            Guard.Argument(endPoint, nameof(endPoint)).NotNull();
            var buildClientPortChunk = endPoint.Port.ToBytesForRLPEncoding();
            Logger.Verbose(string.Join(" ", buildClientPortChunk));
            return buildClientPortChunk;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool ValidateClientId(byte[] clientId)
        {
            return Regex.IsMatch(ByteUtil.ByteToString(clientId), @"^[a-zA-Z]+$");
        }

        /// <summary>
        /// </summary>
        /// <param name="clientVersion"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool ValidateClientVersion(byte[] clientVersion)
        {
            Guard.Argument(clientVersion, nameof(clientVersion))
               .NotNull().NotEmpty().Count(2);
            return clientVersion.ToHex().IsTheSameHex(
                AssemblyMajorVersion2Digits.ToHexUTF8());
        }

        /// <summary>
        /// </summary>
        /// <param name="clientIp"></param>
        private static bool ValidateIp(byte[] clientIp)
        {
            return IPAddress.TryParse(new IPAddress(clientIp).ToString(), out _);
        }

        /// <summary> 
        /// </summary>
        /// <param name="portBytes"></param>
        private static bool ValidatePort(byte[] portBytes)
        {
            return Ip.ValidPortRange(portBytes.ToIntFromRLPDecoded());
        }
    }
}
