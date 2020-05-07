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
using AutoMapper.QueryableExtensions.Impl;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.IO.Transport;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Lib.P2P;
using Serilog;

namespace Catalyst.Core.Lib.P2P
{
    public sealed class PeerClient : UdpClient, IPeerClient
    {
        private readonly IPeerSettings _peerSettings;

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public PeerClient(IUdpClientChannelFactory clientChannelFactory,
            IUdpClientEventLoopGroupFactory eventLoopGroupFactory,
            IPeerSettings peerSettings)
            : base(clientChannelFactory,
                Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType),
                eventLoopGroupFactory)
        {
            _peerSettings = peerSettings;
        }

        public override async Task StartAsync()
        {
            var bindingEndpoint = new IPEndPoint(_peerSettings.BindAddress, IPEndPoint.MinPort);
            var observableChannel = await ChannelFactory.BuildChannelAsync(EventLoopGroupFactory,
                    bindingEndpoint.Address,
                    bindingEndpoint.Port)
               .ConfigureAwait(false);

            var cancellationTokenSource = new CancellationTokenSource();

            //await _pubSubApi.SubscribeAsync("catalyst", msg =>
            //{
            //    if (msg.Sender.Id != _localPeer.Id)
            //    {
            //        var a = 0;
            //        var proto = ProtocolMessage.Parser.ParseFrom(msg.DataStream);
            //        var b = 1;
            //    }
            //}, cancellationTokenSource.Token);

            Channel = observableChannel.Channel;
        }

        public void SendMessageToPeers(IMessage message, IEnumerable<PeerId> peers)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.PeerId);
            foreach (var peer in peers)
            {
                SendMessage(new MessageDto(
                    protocolMessage,
                    peer));
            }
        }
    }
}
