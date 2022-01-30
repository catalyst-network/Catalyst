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

using System.Linq;
using MultiFormats;
using ProtoBuf;

namespace Lib.P2P.Routing
{
    // From https://github.com/libp2p/js-libp2p-kad-dht/blob/master/src/message/dht.proto.js\
    // and https://github.com/libp2p/go-libp2p-kad-dht/blob/master/pb/dht.proto

    /// <summary>
    ///   TODO
    /// </summary>
    [ProtoContract]
    public class DhtRecordMessage
    {
        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(1)]
        public byte[] Key { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(2)]
        public byte[] Value { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(3)]
        public byte[] Author { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(4)]
        public byte[] Signature { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(5)]
        public string TimeReceived { get; set; }
    }

    /// <summary>
    ///   The type of DHT/KAD message.
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        ///   Put a value.
        /// </summary>
        PutValue = 0,

        /// <summary>
        ///   Get a value.
        /// </summary>
        GetValue = 1,

        /// <summary>
        ///   Indicate that a peer can provide something.
        /// </summary>
        AddProvider = 2,

        /// <summary>
        ///   Get the providers for something.
        /// </summary>
        GetProviders = 3,

        /// <summary>
        ///   Find a peer.
        /// </summary>
        FindNode = 4,

        /// <summary>
        ///   NYI
        /// </summary>
        Ping = 5
    }

    /// <summary>
    ///   The connection status.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Sender does not have a connection to peer, and no extra information (default)
        /// </summary>
        NotConnected = 0,

        /// <summary>
        /// Sender has a live connection to peer
        /// </summary>
        Connected = 1,

        /// <summary>
        /// Sender recently connected to peer
        /// </summary>
        CanConnect = 2,

        /// <summary>
        /// Sender recently tried to connect to peer repeatedly but failed to connect
        /// ("try" here is loose, but this should signal "made strong effort, failed")
        /// </summary>
        CannotConnect = 3
    }

    /// <summary>
    ///   Information about a peer.
    /// </summary>
    [ProtoContract]
    public class DhtPeerMessage
    {
        /// <summary>
        /// ID of a given peer. 
        /// </summary>
        /// <value>
        ///   The <see cref="MultiHash"/> as a byte array,
        /// </value>
        [ProtoMember(1)]
        public byte[] Id { get; set; }

        /// <summary>
        /// Addresses for a given peer
        /// </summary>
        /// <value>
        ///   A sequence of <see cref="MultiAddress"/> as a byte array.
        /// </value>
        [ProtoMember(2)]
        public byte[][] Addresses { get; set; }

        /// <summary>
        /// used to signal the sender's connection capabilities to the peer
        /// </summary>
        [ProtoMember(3)]
        public ConnectionType Connection { get; set; }

        /// <summary>
        ///   Convert the message into a <see cref="Peer"/>.
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public bool TryToPeer(out Peer peer)
        {
            peer = null;

            // Sanity checks.
            if (Id == null || Id.Length == 0)
                return false;

            MultiHash id = new(Id);
            peer = new Peer
            {
                Id = id
            };
            if (Addresses != null)
            {
                MultiAddress x = new($"/ipfs/{id}");
                peer.Addresses = Addresses
                   .Select(bytes =>
                    {
                        try
                        {
                            MultiAddress ma = new(bytes);
                            ma.Protocols.AddRange(x.Protocols);
                            return ma;
                        }
                        catch
                        {
                            return null;
                        }
                    })
                   .Where(a => a != null)
                   .ToArray();
            }

            return true;
        }
    }

    /// <summary>
    ///   The DHT message exchanged between peers.
    /// </summary>
    [ProtoContract]
    public class DhtMessage
    {
        /// <summary>
        /// What type of message it is.
        /// </summary>
        [ProtoMember(1)]
        public MessageType Type { get; set; }

        /// <summary>
        ///   Coral cluster level.
        /// </summary>
        [ProtoMember(10)]
        public int ClusterLevelRaw { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(2)]
        public byte[] Key { get; set; }

        /// <summary>
        ///   TODO
        /// </summary>
        [ProtoMember(3)]
        public DhtRecordMessage Record { get; set; }

        /// <summary>
        ///   The closer peers for a query.
        /// </summary>
        [ProtoMember(8)]
        public DhtPeerMessage[] CloserPeers { get; set; }

        /// <summary>
        ///  The providers for a query.
        /// </summary>
        [ProtoMember(9)]
        public DhtPeerMessage[] ProviderPeers { get; set; }
    }
}
