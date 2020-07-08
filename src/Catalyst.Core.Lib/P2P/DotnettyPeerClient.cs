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

using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.IO.Transport;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.P2P
{
    public sealed class DotnettyPeerClient : UdpClient, IPeerClient, ISocketClient
    {
        private readonly IPeerSettings _peerSettings;
        private readonly IBroadcastManager _broadcastManager;

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public DotnettyPeerClient(IUdpClientChannelFactory clientChannelFactory,
            IUdpClientEventLoopGroupFactory eventLoopGroupFactory,
            IBroadcastManager broadcastManager,
            IPeerSettings peerSettings)
            : base(clientChannelFactory,
                Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType),
                eventLoopGroupFactory)
        {
            _broadcastManager = broadcastManager;
            _peerSettings = peerSettings;
        }

        public override async Task StartAsync()
        {
            await StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var observableChannel = await ChannelFactory.BuildChannelAsync(EventLoopGroupFactory, _peerSettings.Address).ConfigureAwait(false);
            Channel = observableChannel.Channel;
        }

        public Task SendMessageToPeersAsync(IMessage message, IEnumerable<MultiAddress> peers)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.Address);
            foreach (var peer in peers)
            {
                SendMessage(new MessageDto(
                    protocolMessage,
                    peer));
            }
            return Task.CompletedTask;
        }

        public Task SendMessageAsync<T>(IMessageDto<T> message) where T : IMessage<T>
        {
            SendMessage(message);
            return Task.CompletedTask;
        }

        public async Task BroadcastAsync(ProtocolMessage message)
        {
            await _broadcastManager.BroadcastAsync(message).ConfigureAwait(false);
        }
    }
}
