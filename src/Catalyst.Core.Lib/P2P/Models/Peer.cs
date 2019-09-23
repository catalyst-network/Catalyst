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
using System.ComponentModel.DataAnnotations;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Repository.Attributes;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.P2P.Models
{
    [Audit]
    public sealed class Peer : IPeer
    {
        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        [BsonId]
        public string DocumentId => PeerId?.ToByteString().ToBase64();
        
        /// <inheritdoc />
        public PeerId PeerId { get; set; }
            
        /// <inheritdoc />
        public int Reputation { get; set; }

        /// <inheritdoc />
        public bool BlackListed { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     When peer was first seen by the peer.
        /// </summary>
        public DateTime Created { get; set; }

        /// <inheritdoc />
        public DateTime? Modified { get; set; }

        /// <inheritdoc />
        public DateTime LastSeen { get; set; }

        /// <inheritdoc />
        public bool IsAwolPeer => InactiveFor > TimeSpan.FromMinutes(30);

        /// <inheritdoc />
        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        /// <inheritdoc />
        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }
    }

    [Audit]
    public class PeerDao 
    {
        [RepositoryPrimaryKey(Order = 1)]
        [Key]
        public string Id { get; set; }

        /// <inheritdoc />
        public PeerIdDao PeerIdentifier { get; set; }

        /// <inheritdoc />
        public int Reputation { get; set; }

        /// <inheritdoc />
        public bool BlackListed { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     When peer was first seen by the peer.
        /// </summary>
        public DateTime Created { get; set; }

        /// <inheritdoc />
        public DateTime? Modified { get; set; }

        /// <inheritdoc />
        public DateTime LastSeen { get; set; }

        /// <inheritdoc />
        public bool IsAwolPeer => InactiveFor > TimeSpan.FromMinutes(30);

        /// <inheritdoc />
        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        /// <inheritdoc />
        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }

        public PeerDao ToPeerDao(Peer peer)
        {
            var tempPeerDao = new PeerIdDao
            {
                Ip = peer.PeerId.ToString(),
                Port = (int) peer.PeerId.Port,
                PublicKey = peer.PeerId.PublicKey.KeyToString()
            };

            return new PeerDao
            {
                PeerIdentifier = tempPeerDao,
                Reputation = peer.Reputation,
                BlackListed = peer.BlackListed,
                Created = peer.Created,
                Modified = peer.Modified,
                LastSeen = peer.LastSeen,
            };
        }
    }
}
