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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Cryptography.Proto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Peer;
using MultiFormats;
using NSubstitute;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using ProtoBuf;

namespace Catalyst.TestUtils
{
    public static class PeerIdHelper
    {
        private static string AddPublicKeySubjectInfo(byte[] publicKeyBytes)
        {
            var publicKey = new Ed25519PublicKeyParameters(publicKeyBytes, 0);
            var pksi = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey).GetDerEncoded();
            var pk = new PublicKey
            {
                Type = KeyType.Ed25519,
                Data = pksi
            };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, pk);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string AddPublicKeySubjectInfo(AsymmetricKeyParameter publicKey)
        {
            var publicKeyBytes = (Ed25519PublicKeyParameters) publicKey;
            return AddPublicKeySubjectInfo(publicKeyBytes.GetEncoded());
        }

        public static PeerId GetPeerId(byte[] publicKey = null,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            if (publicKey == null)
            {
                var g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
                g.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
                var keyPair = g.GenerateKeyPair();
                publicKey = ((Ed25519PublicKeyParameters) keyPair.Public).GetEncoded();
            }

            //var publicKey64 = AddPublicKeySubjectInfo(publicKey);
            var peerIdentifier = new PeerId
            {
                PublicKey = publicKey.ToByteString(),
                Ip = (ipAddress ?? IPAddress.Loopback).To16Bytes().ToByteString(),
                Port = (ushort) port
            };
            return peerIdentifier;
        }

        public static PeerId GetPeerId(string publicKeySeed,
            IPAddress ipAddress = null,
            int port = 12345)
        {
            var g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
            g.Init(new Ed25519KeyGenerationParameters(new SecureRandom(Encoding.UTF8.GetBytes(publicKeySeed))));
            var keyPair = g.GenerateKeyPair();
            var publicKeyBytes = ((Ed25519PublicKeyParameters) keyPair.Public).GetEncoded();
            //AddPublicKeySubjectInfo(keyPair.Public);
            //var publicKeyBytes = Encoding.UTF8.GetBytes(publicKeySeed)
            //   .Concat(Enumerable.Repeat(default(byte), new FfiWrapper().PublicKeyLength))
            //   .Take(new FfiWrapper().PublicKeyLength).ToArray();
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
