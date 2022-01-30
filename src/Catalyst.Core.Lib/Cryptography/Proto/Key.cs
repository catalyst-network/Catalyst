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

using ProtoBuf;

namespace Catalyst.Core.Lib.Cryptography.Proto
{
    public enum KeyType
    {
        Rsa = 0,
        Ed25519 = 1,
        Secp256K1 = 2,
        Ecdh = 4,
    }

    [ProtoContract]
    public sealed class PublicKey
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type;

        [ProtoMember(2, IsRequired = true)]
        public byte[] Data;
    }

    // PrivateKey message is not currently used.  Hopefully it never will be
    // because it could introduce a huge security hole.
#if false
    [ProtoContract]
    class PrivateKey
    {
        [ProtoMember(1, IsRequired = true)]
        public KeyType Type;
        [ProtoMember(2, IsRequired = true)]
        public byte[] Data;
    }
#endif
}
