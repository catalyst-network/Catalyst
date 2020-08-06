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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Google.Protobuf;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Evm;

namespace Catalyst.Core.Modules.Kvm
{
    public static class PublicKeyExtensions
    {
        public static Address ToKvmAddress(this IPublicKey publicKey)
        {
            if (publicKey == null)
            {
                return null;
            }

            return ToKvmAddress(publicKey.Bytes);
        }

        public static Address ToKvmAddress(this byte[] publicKey)
        {
            return new Address(ValueKeccak.Compute(publicKey).BytesAsSpan.SliceWithZeroPadding(0, 20).ToArray());
        }

        public static ByteString ToKvmAddressByteString(this IPublicKey recipient)
        {
            return recipient?.ToKvmAddress().Bytes.ToByteString() ?? ByteString.Empty;
        }

        public static ByteString ToKvmAddressByteString(this byte[] publicKey)
        {
            return publicKey?.ToKvmAddress().Bytes.ToByteString() ?? ByteString.Empty;
        }
    }
}
