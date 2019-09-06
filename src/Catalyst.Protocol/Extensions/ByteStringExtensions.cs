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
using Catalyst.Common.Extensions;
using Google.Protobuf;
using Multiformats.Hash;

namespace Catalyst.Protocol.Extensions
{
    public static class ByteStringExtensions
    {
        public static ByteString ToByteString(this Guid guid) { return guid.ToByteArray().ToByteString(); }

        public static Multihash AsMultihash(this ByteString byteString)
        {
            return Multihash.Decode(byteString.ToByteArray());
        }

        public static string AsBase32Address(this ByteString byteString)
        {
            return AsMultihash(byteString).AsBase32Address();
        }
    }
}
