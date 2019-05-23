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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Network;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RLP;

namespace Catalyst.Common.P2P
{
    /// <summary>
    ///     Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
    ///     the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4
    ///     the leading 12 bytes should be padded 0x0
    ///     clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
    ///     The client ID for this implementation is "AC" or hexadecimal 4143
    /// </summary>
    public sealed class PeerIdentifier : IPeerIdentifier
    {
        private static readonly string AssemblyMajorVersion2Digits = Assembly.GetExecutingAssembly().GetName().Version.Major.ToString("D2");
        private static readonly byte[] AssemblyMajorVersion2Bytes = Encoding.UTF8.GetBytes(AssemblyMajorVersion2Digits);
        private static readonly byte[] AtlasClientId = Encoding.UTF8.GetBytes("AC");

        public string ClientId => PeerId.ClientId.ToStringUtf8();
        public string ClientVersion => PeerId.ClientVersion.ToStringUtf8();
        public IPAddress Ip => new IPAddress(PeerId.Ip.ToByteArray()).MapToIPv4();
        public int Port => BitConverter.ToUInt16(PeerId.Port.ToByteArray());
        public byte[] PublicKey => PeerId.PublicKey.ToByteArray();
        public IPEndPoint IpEndPoint => EndpointBuilder.BuildNewEndPoint(Ip, Port);
        public PeerId PeerId { get; }

        public PeerIdentifier(PeerId peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).Require(ValidatePeerId);
            PeerId = peerId;
        }

        /// <summary>
        ///     Parses a hex string containing the chunks that make up a valid PeerIdentifier that are delimited by '|'
        /// </summary>
        /// <param name="rawPidChunks"></param>
        /// <returns></returns>
        internal static PeerIdentifier ParseHexPeerIdentifier(IReadOnlyList<string> rawPidChunks)
        {
            Guard.Argument(rawPidChunks).Count(5);
            Guard.Argument(rawPidChunks[0]).Length(2);
            Guard.Argument(rawPidChunks[1]).Length(2);
            Guard.Argument(rawPidChunks[2]).Length(14);
            Guard.Argument(rawPidChunks[3]).MinLength(4).MaxLength(5);
            Guard.Argument(rawPidChunks[4]).Length(20);
            
            var peerByteChunks = new List<ByteString>();
            rawPidChunks.ToList().ForEach(chunk => peerByteChunks.Add(chunk.ToBytesForRLPEncoding().ToByteString()));

            return new PeerIdentifier(new PeerId
            {
                ClientId = peerByteChunks[0],
                ClientVersion = peerByteChunks[1],
                Ip = IPAddress.Parse(rawPidChunks[2]).MapToIPv4().To16Bytes().ToByteString(),
                Port = peerByteChunks[3],
                PublicKey = peerByteChunks[4]
            });
        }
        
        public PeerIdentifier(IPeerSettings settings, IKeySigner keySigner)
            : this(keySigner.GetPublicKey().ToBytesForRLPEncoding(), new IPEndPoint(settings.BindAddress.MapToIPv4(), settings.Port)) { }
        
        public PeerIdentifier(IEnumerable<byte> publicKey, IPAddress ipAddress, int port)
            : this(publicKey, EndpointBuilder.BuildNewEndPoint(ipAddress, port)) { }
        
        private PeerIdentifier(IEnumerable<byte> publicKey, IPEndPoint endPoint)
        {
            PeerId = new PeerId
            {
                PublicKey = publicKey.ToByteString(),
                Port = BitConverter.GetBytes(endPoint.Port).ToByteString(),
                Ip = endPoint.Address.To16Bytes().ToByteString(),
                ClientId = AtlasClientId.ToByteString(),
                ClientVersion = AssemblyMajorVersion2Bytes.ToByteString()
            };
        }

        private static bool ValidatePeerId(PeerId peerId)
        {
            Guard.Argument(peerId, nameof(peerId)).NotNull()
               .Require(p => p.PublicKey.Length == 20, _ => "PublicKey should be 20 bytes")
               .Require(p => p.Ip.Length == 16 && ValidateIp(p.Ip.ToByteArray()), _ => "Ip should be 16 bytes")
               .Require(p => ValidatePort(p.Port.ToByteArray()), _ => "Port should be between 1025 and 65535")
               .Require(p => ValidateClientId(p.ClientId.ToByteArray()),
                    _ => "ClientId should only be 2 alphabetical letters")
               .Require(p => ValidateClientVersion(p.ClientVersion.ToByteArray()),
                    _ => $"ClientVersion doesn't match {AssemblyMajorVersion2Digits}");
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool ValidateClientId(byte[] clientId)
        {
            return Regex.IsMatch(ByteUtil.ByteToString(clientId), @"^[a-zA-Z]{1,2}$");
        }

        /// <summary>
        /// </summary>
        /// <param name="clientVersion"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool ValidateClientVersion(byte[] clientVersion)
        {
            Guard.Argument(clientVersion, nameof(clientVersion))
               .NotNull().NotEmpty().Count(2);
            var intVersion = int.Parse(Encoding.UTF8.GetString(clientVersion));
            return 0 <= intVersion && intVersion <= 99;
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
            return Common.Network.Ip.ValidPortRange(BitConverter.ToUInt16(portBytes));
        }

        public override string ToString()
        {
            return ClientId + ClientVersion + $"@{Ip}:{Port}" + $"|{PublicKey.ToHex()}";
        }

        public bool Equals(IPeerIdentifier other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            
            return Equals(PeerId, other.PeerId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            
            return obj is IPeerIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return PeerId != null ? PeerId.GetHashCode() : 0;
        }
    }
}
