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
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.P2P;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Common.P2P
{
    public sealed class Peer : IDisposable, IPeer
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>Gets or sets the primary key identifier.</summary>
        /// <value>The primary key identifier.</value>
        [RepositoryPrimaryKey(Order = 1)]
        public int PkId { get; set; }

        /// <summary>Gets the reputation.</summary>
        /// <value>The reputation.</value>
        public int Reputation { get; set; }

        /// <summary>Gets the last seen.</summary>
        /// <value>The last seen.</value>
        public DateTime LastSeen { get; set; }

        /// <summary>Gets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        public IPeerIdentifier PeerIdentifier { get; set; }

        /// <summary>Gets a value indicating whether this instance is awol peer.</summary>
        /// <value><c>true</c> if this instance is awol peer; otherwise, <c>false</c>.</value>
        public bool IsAwolPeer => InactiveFor > TimeSpan.FromMinutes(30);

        /// <summary>Gets the inactive for.</summary>
        /// <value>The inactive for.</value>
        public TimeSpan InactiveFor => DateTimeUtil.UtcNow - LastSeen;

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() { Dispose(true); }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Touch() { LastSeen = DateTimeUtil.UtcNow; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void IncreaseReputation(int mer = 1)
        {
            Reputation += mer;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void DecreaseReputation(int mer = 1)
        {
            Reputation += mer;
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Verbose("Connection to peer {0} Disposed.",
                    PeerIdentifier?.ToString() ?? "unknown");
            }
        }
    }
}
