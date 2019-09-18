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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;

namespace Catalyst.Core.Lib.P2P
{
    /// <summary>
    ///     Peer ID's should return a unsigned 42 byte array in the following format, to produce a 336 bit key space
    ///     the ip chunk is 16 bytes long to account for ipv6 addresses, ipv4 addresses are only 4bytes long, in case of ipv4
    ///     the leading 12 bytes should be padded 0x0
    ///     clientID [2] + clientVersion[2] + Ip[16] + Port[2] + pub[20]
    ///     The client ID for this implementation is "AC" or hexadecimal 4143
    /// </summary>
    /// <summary>
    ///     @TODO move to SDK
    /// </summary>
    [Obsolete("Please try to use the PeerId directly")]
    public sealed class PeerIdentifier : IPeerIdentifier
    {
        public static char PidDelimiter => '|';
        public PeerId PeerId { get; }
        public IPAddress Ip => new IPAddress(PeerId.Ip.ToByteArray()).MapToIPv4();
        public int Port => (int) PeerId.Port;
        public byte[] PublicKey => PeerId.PublicKey.ToByteArray();
        public IPEndPoint IpEndPoint => EndpointBuilder.BuildNewEndPoint(Ip, Port);
        
        public PeerIdentifier(PeerId peerId)
        {
            // hard coding this for now as this class should be replaced by dao,
            // requiring bullet proof FFI violates DIP and creates circular dep between Core.Lib and Cryptography.BulletProof
            // due to nature of FFI not sure we can inject it here
            // var keyLength = FFI.PublicKeyLength;
            var keyLength = 32;
            Guard.Argument(peerId.PublicKey, nameof(peerId.PublicKey)).MinCount(keyLength).MaxCount(keyLength);
            PeerId = peerId;
        }

        /// <summary>
        ///     Parses a hex string containing the chunks that make up a valid PeerId that are delimited by '|'
        /// </summary>
        /// <param name="rawPidChunks"></param>
        /// <returns></returns>
        public static PeerIdentifier ParseHexPeerIdentifier(IReadOnlyList<string> rawPidChunks)
        {
            var peerByteChunks = new List<ByteString>();
            rawPidChunks.ToList().ForEach(chunk => peerByteChunks.Add(chunk.ToBytesForRLPEncoding().ToByteString()));

            return new PeerIdentifier(new PeerId
            {
                Ip = IPAddress.Parse(rawPidChunks[2]).MapToIPv4().To16Bytes().ToByteString(),
                Port = uint.Parse(rawPidChunks[3]),
                PublicKey = peerByteChunks[4]
            });
        }

        public PeerIdentifier(IPeerSettings settings) : this(settings.PublicKey.KeyToBytes(), 
            new IPEndPoint(settings.PublicIpAddress, settings.Port)) { }

        public PeerIdentifier(IEnumerable<byte> publicKey, IPAddress ipAddress, int port)
            : this(publicKey, EndpointBuilder.BuildNewEndPoint(ipAddress, port)) { }
        
        private PeerIdentifier(IEnumerable<byte> publicKey, IPEndPoint endPoint)
        {
            PeerId = new PeerId
            {
                PublicKey = publicKey.ToByteString(),
                Port = (uint) endPoint.Port,
                Ip = endPoint.Address.To16Bytes().ToByteString()
            };
        }

        public override string ToString()
        {
            return $"{Ip}:{Port.ToString()}" + $"|{PublicKey.KeyToString()}";
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
