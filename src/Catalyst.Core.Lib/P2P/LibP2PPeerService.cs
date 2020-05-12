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

using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P
{
    public class LibP2PPeerService : ILibP2PPeerService
    {
        private readonly IPeerSettings _peerSettings;
        private readonly IPubSubApi _pubSubApi;
        private readonly PeerLibP2PServerChannelFactory _peerLibP2PChannelFactory;
        private readonly IEnumerable<IP2PMessageObserver> _messageHandlers;

        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { private set; get; }

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public LibP2PPeerService(IPeerSettings peerSettings, 
            PeerLibP2PServerChannelFactory peerLibP2PChannelFactory, 
            IEnumerable<IP2PMessageObserver> messageHandlers)
        {
            _peerSettings = peerSettings;
            _peerLibP2PChannelFactory = peerLibP2PChannelFactory;
            _messageHandlers = messageHandlers;
        }

        public async Task StartAsync()
        {
            MessageStream = await _peerLibP2PChannelFactory.BuildMessageStreamAsync();
            _messageHandlers.ToList().ForEach(h => h.StartObserving(MessageStream));
        }
    }
}