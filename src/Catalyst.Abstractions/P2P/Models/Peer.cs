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
using Catalyst.Abstractions.Lib.Util;
using Catalyst.Abstractions.P2P.Models;
using Catalyst.Abstractions.Service.Attributes;
using MultiFormats;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.P2P.Models
{
    [Audit]
    public sealed class Peer : IPeer
    {
        [RepositoryPrimaryKey(Order = 1)]
        public string DocumentId => Address.ToString();
        
        /// <inheritdoc />
        public MultiAddress Address { get; set; }

        public bool IsPoaNode { set; get; }

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
}
