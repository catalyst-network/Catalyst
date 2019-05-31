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
using System.Collections.Generic;
using System.Net;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Node.Core.P2P.Messaging.Gossip;

namespace Catalyst.Node.Core.P2P
{
    /// <summary>
    /// This <inheritdoc cref="PeerClientFactory"/> class initializes the peer client
    /// </summary>
    /// <seealso cref="IPeerClientFactory" />
    public class PeerClientFactory : IPeerClientFactory
    {
        /// <inheritdoc cref="IPeerClientFactory"/>
        public IPeerClient Client { get; set; }

        /// <summary>The target host</summary>
        private readonly IPEndPoint _targetHost;

        /// <summary>The gossip manager context</summary>
        private readonly IGossipManagerContext _gossipManagerContext;

        /// <summary>Initializes a new instance of the <see cref="PeerClientFactory"/> class.</summary>
        /// <param name="peerSettings">The peer settings.</param>
        /// <param name="gossipManagerContext">The gossip manager context.</param>
        public PeerClientFactory(IPeerSettings peerSettings, IGossipManagerContext gossipManagerContext)
        {
            _gossipManagerContext = gossipManagerContext;
            _targetHost = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port);
        }

        /// <inheritdoc cref="IPeerClientFactory"/>
        public void Initialize(IEnumerable<IP2PMessageHandler> messageHandlers)
        {
            Client = new PeerClient(_targetHost, messageHandlers, new GossipManager(this, _gossipManagerContext));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <inheritdoc cref="IDisposable"/>
        protected virtual void Dispose(bool disposing)
        {
            Client?.Dispose();
        }
    }
}
