#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Core.Lib.Util;
using Google.Protobuf;
using MultiFormats;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Evm;
using System.Net;

namespace Catalyst.Core.Lib.Extensions
{
    public static class MultiAddressExtensions
    {
        public static IPAddress GetIpAddress(this MultiAddress address)
        {
            return IPAddress.Parse(address.Protocols[0].Value);
        }

        public static int GetPort(this MultiAddress address)
        {
            return int.Parse(address.Protocols[1].Value);
        }

        public static string GetPublicKey(this MultiAddress address)
        {
            return address.GetPublicKeyBytes().KeyToString();
        }

        public static byte[] GetPublicKeyBytes(this MultiAddress address)
        {
            return address.PeerId.GetPublicKeyBytesFromPeerId();
        }

        public static Address GetKvmAddress(this MultiAddress address)
        {
            return new Address(ValueKeccak.Compute(GetPublicKeyBytes(address)).BytesAsSpan.SliceWithZeroPadding(0, 20).ToArray());
        }

        public static ByteString GetKvmAddressByteString(this MultiAddress address)
        {
            return GetKvmAddress(address).Bytes.ToByteString() ?? ByteString.Empty;
        }

        public static IPEndPoint GetIPEndPoint(this MultiAddress address)
        {
            return new IPEndPoint(address.GetIpAddress(), address.GetPort());
        }
    }
}
