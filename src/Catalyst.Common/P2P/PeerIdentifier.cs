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
using System.Text;
using Catalyst.Common.Network;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.RPC;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
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
        public string ClientId => PeerId.ClientId.ToStringUtf8();
        public string ClientVersion => PeerId.ClientVersion.ToStringUtf8();
        public IPAddress Ip => new IPAddress(PeerId.Ip.ToByteArray()).MapToIPv4();
        public int Port => BitConverter.ToUInt16(PeerId.Port.ToByteArray());
        public byte[] PublicKey => PeerId.PublicKey.ToByteArray();
        public IPEndPoint IpEndPoint => EndpointBuilder.BuildNewEndPoint(Ip, Port);
        public PeerId PeerId { get; }

        public PeerIdentifier(PeerId peerId)
        {
            PeerId = peerId;
        }

        public static IPeerIdentifier BuildPeerIdFromConfig(IRpcNodeConfig nodeConfig, IPeerIdClientId clientId)
        {
            return new PeerIdentifier(Encoding.ASCII.GetBytes(nodeConfig.PublicKey),
                nodeConfig.HostAddress, nodeConfig.Port, clientId);
        }
        
        public static IPeerIdentifier BuildPeerIdFromConfig(IConfiguration configuration, IPeerIdClientId clientId)
        {
            //TODO: Handle different scenarios to get the IPAddress and Port depending
            //on you whether you are connecting to a local node, or a remote one.
            //https://github.com/catalyst-network/Catalyst.Node/issues/307

            return new PeerIdentifier(configuration.GetSection("CatalystCliConfig")
                   .GetSection("PublicKey").Value.ToBytesForRLPEncoding(),
                IPAddress.Loopback, IPEndPoint.MaxPort, clientId);
        }

        /// <summary>
        ///     Parses a hex string containing the chunks that make up a valid PeerIdentifier that are delimited by '|'
        /// </summary>
        /// <param name="rawPidChunks"></param>
        /// <returns></returns>
        internal static PeerIdentifier ParseHexPeerIdentifier(IReadOnlyList<string> rawPidChunks)
        {
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
        
        public PeerIdentifier(IPeerSettings settings, IPeerIdClientId clientId)
            : this(settings.PublicKey.ToBytesForRLPEncoding(), new IPEndPoint(settings.BindAddress.MapToIPv4(), settings.Port), clientId) { }
        
        public PeerIdentifier(IEnumerable<byte> publicKey, IPAddress ipAddress, int port, IPeerIdClientId clientId)
            : this(publicKey, EndpointBuilder.BuildNewEndPoint(ipAddress, port), clientId) { }
        
        private PeerIdentifier(IEnumerable<byte> publicKey, IPEndPoint endPoint, IPeerIdClientId clientId)
        {
            PeerId = new PeerId
            {
                PublicKey = publicKey.ToByteString(),
                Port = BitConverter.GetBytes(endPoint.Port).ToByteString(),
                Ip = endPoint.Address.To16Bytes().ToByteString(),
                ClientId = clientId.ClientVersion.ToByteString(),
                ClientVersion = clientId.AssemblyMajorVersion.ToByteString()
            };
        }

        public override string ToString()
        {
            return ClientId + ClientVersion + $"@{Ip}:{Port.ToString()}" + $"|{PublicKey.ToHex()}";
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
