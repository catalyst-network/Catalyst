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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.IO.Transport.Channels;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Lib.P2P.Protocols;
using MultiFormats;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P
{
    public class LibP2PPeerClient : ILibP2PPeerClient
    {
        private readonly IPeerSettings _peerSettings;
        private readonly IPubSubApi _pubSubApi;
        private readonly ICatalystProtocol _catalystProtocol;
        private readonly PeerLibP2PClientChannelFactory _peerLibP2PChannelFactory;

        public IObservable<ProtocolMessage> MessageStream { private set; get; }

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public LibP2PPeerClient(IPeerSettings peerSettings, PeerLibP2PClientChannelFactory peerLibP2PChannelFactory, IPubSubApi pubSubApi, ICatalystProtocol catalystProtocol)
        {
            _peerSettings = peerSettings;
            _peerLibP2PChannelFactory = peerLibP2PChannelFactory;
            _pubSubApi = pubSubApi;
            _catalystProtocol = catalystProtocol;
        }

        public async Task StartAsync()
        {
            MessageStream = await _peerLibP2PChannelFactory.BuildMessageStreamAsync();
        }

        public async Task SendMessageToPeersAsync<T>(T message, IEnumerable<MultiAddress> peers) where T : IMessage<T>
        {
            foreach (var peer in peers)
            {
                await SendMessageAsync(message, peer);
            }
        }

        public async Task SendMessageAsync<T>(T message, MultiAddress recipient) where T : IMessage<T>
        {
            try
            {
                await _catalystProtocol.SendAsync(recipient, message.ToProtocolMessage(_peerSettings.Address)).ConfigureAwait(false);
            }
            catch(Exception exc)
            {
                var a = exc;
            }
        }

        public async Task BroadcastAsync(ProtocolMessage message)
        {
            await _pubSubApi.PublishAsync("catalyst", message.ToByteArray());
        }
    }
}
