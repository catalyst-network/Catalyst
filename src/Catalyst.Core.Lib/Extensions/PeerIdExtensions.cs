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

using Catalyst.Core.Lib.Cryptography.Proto;
using MultiFormats;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using ProtoBuf;
using System;
using System.IO;

namespace Catalyst.Core.Lib.Extensions
{
    public class PublicKeyExtractionException : Exception
    {
        public PublicKeyExtractionException(string message) : base(message)
        {

        }
    }

    public static class PeerIdExtensions
    {
        public static byte[] GetPublicKeyBytesFromPeerId(this byte[] peerIdBytes)
        {
            try
            {
                var peerId = new MultiHash("id", peerIdBytes);
                using var ms = new MemoryStream(peerId.Digest);
                var publicKey = Serializer.Deserialize<PublicKey>(ms);
                using var aIn = new Asn1InputStream(publicKey.Data);
                var info = SubjectPublicKeyInfo.GetInstance(aIn.ReadObject());
                return info.PublicKeyData.GetBytes();
            }
            catch (Exception)
            {
                throw new PublicKeyExtractionException("Could not extract public key from peerId");
            }
        }

        public static string ToPeerId(this byte[] publicKeyBytes)
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

        public static string ToPeerId(this AsymmetricKeyParameter publicKeyParameter)
        {
            var ed25519PublicKeyParameter = (Ed25519PublicKeyParameters) publicKeyParameter;
            return ToPeerId(ed25519PublicKeyParameter.GetEncoded());
        }

    }
}
