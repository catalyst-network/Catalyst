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
using System.Reflection;
using Catalyst.Common.Interfaces.Attributes;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Attributes;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Common.P2P
{
    [Audit]
    public sealed class Peer : IPeer, IAuditable
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <inheritdoc />
        [RepositoryPrimaryKey(Order = 1)]
        public int PkId { get; set; }

        /// <inheritdoc />
        public int Reputation { get; set; }

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
        public IPeerIdentifier PeerIdentifier { get; set; }

        /// <inheritdoc />
        public bool IsAwolPeer => InactiveFor > TimeSpan.FromMinutes(30);

        /// <inheritdoc />
        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        /// <inheritdoc />
        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }

        /// <inheritdoc />
        public void IncreaseReputation(int mer = 1)
        {
            Reputation += mer;
        }

        /// <inheritdoc />
        public void DecreaseReputation(int mer = 1)
        {
            Reputation += mer;
        }
    }
}
