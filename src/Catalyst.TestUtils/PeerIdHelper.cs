#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using System.Linq;
using System.Net;
using System.Text;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class PeerIdHelper
    {
        public static PeerId GetPeerId(byte[] publicKey = null,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            var peerIdentifier = new PeerId
            {
                PublicKey = (publicKey ?? new byte[32]).ToByteString(),
                Ip = (ipAddress ?? IPAddress.Loopback).To16Bytes().ToByteString(),
                Port = (ushort) port
            };
            return peerIdentifier;
        }

        public static PeerId GetPeerId(string publicKeySeed,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            var publicKeyBytes = Encoding.UTF8.GetBytes(publicKeySeed)
               .Concat(Enumerable.Repeat(default(byte), new FfiWrapper().PublicKeyLength))
               .Take(new FfiWrapper().PublicKeyLength).ToArray();
            return GetPeerId(publicKeyBytes, ipAddress, port);
        }

        public static PeerId GetPeerId(string publicKey, string ipAddress, int port)
        {
            return GetPeerId(publicKey, IPAddress.Parse(ipAddress), port);
        }

        public static IPeerSettings ToSubstitutedPeerSettings(this PeerId peerId)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.PeerId.Returns(peerId);
            peerSettings.BindAddress.Returns(peerId.IpAddress);
            peerSettings.Port.Returns((int) peerId.Port);
            peerSettings.PublicKey.Returns(peerId.PublicKey.KeyToString());
            return peerSettings;
        }
    }
}
