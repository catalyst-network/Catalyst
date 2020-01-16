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

using ProtoBuf;

/* From https://github.com/libp2p/js-libp2p-secio/blob/master/src/handshake/secio.proto.js
 
module.exports = `message Propose
{
    optional bytes rand = 1;
    optional bytes pubkey = 2;
    optional string exchanges = 3;
    optional string ciphers = 4;
    optional string hashes = 5;
}
message Exchange
{
    optional bytes epubkey = 1;
    optional bytes signature = 2;
}
*/

namespace Lib.P2P.SecureCommunication
{
    [ProtoContract]
    internal class Secio1Propose
    {
        [ProtoMember(1)]
        public byte[] Nonce;

        [ProtoMember(2)]
        public byte[] PublicKey;

        [ProtoMember(3)]
        public string Exchanges;

        [ProtoMember(4)]
        public string Ciphers;

        [ProtoMember(5)]
        public string Hashes;
    }

    [ProtoContract]
    internal class Secio1Exchange
    {
        [ProtoMember(1)]
        public byte[] EPublicKey;

        [ProtoMember(2)]
        public byte[] Signature;
    }
}
