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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using Catalyst.Core.Lib.Network;
using Catalyst.Protocol.Peer;
using Dawn;
using Google.Protobuf;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Lib.Extensions
{
    public static class KeccakExtensions
    {
        public static ByteString ToByteString(this Keccak keccak)
        {
            return keccak == null ? ByteString.Empty : ByteString.CopyFrom(keccak.Bytes);
        }
        
        public static Keccak ToKeccak(this ByteString byteString)
        {
            return (byteString == null || byteString.IsEmpty) ? null : new Keccak(byteString.ToByteArray());
        }
    }
    
    public static class BytesExtensions
    {
        public static MemoryStream ToMemoryStream(this byte[] content)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(content, 0, content.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public static ByteString ToByteString(this IEnumerable<byte> bytes)
        {
            var enumerable = bytes as byte[] ?? bytes.ToArray();
            return ByteString.CopyFrom(enumerable);
        }

        public static UInt256 ToUInt256(this ByteString byteString)
        {
            var bytes = byteString.ToArray();
            return new UInt256(new BigInteger(bytes));
        }

        public static ByteString ToUint256ByteString(this UInt256 uInt256)
        {
            return ((BigInteger) uInt256).ToByteArray().ToByteString();
        }

        public static ByteString ToUint256ByteString(this ulong uLong)
        {
            return ((BigInteger) uLong).ToByteArray().ToByteString();
        }

        public static ByteString ToUint256ByteString(this uint uInt)
        {
            return ((BigInteger) uInt).ToByteArray().ToByteString();
        }

        public static ByteString ToUint256ByteString(this int @int)
        {
            return ((BigInteger) @int).ToByteArray().ToByteString();
        }

        public static PeerId BuildPeerIdFromPublicKey(this byte[] publicKey, IPEndPoint ipEndPoint)
        {
            return BuildPeerIdFromPublicKey(publicKey, ipEndPoint.Address, ipEndPoint.Port);
        }

        public static PeerId BuildPeerIdFromPublicKey(this byte[] publicKey, IPAddress ipAddress, int port)
        {
            Guard.Argument(publicKey, nameof(publicKey)).NotNull().NotEmpty();
            return new PeerId
            {
                PublicKey = publicKey.ToByteString(),
                Ip = ipAddress.To16Bytes().ToByteString(),
                Port = (uint) port
            };
        }
    }
}
