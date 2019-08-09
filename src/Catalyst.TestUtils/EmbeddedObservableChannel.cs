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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.IO.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public static class DotNettyExtensions
    {
        public static IChannelId ToChannelId(this string channelName)
        {
            var channelId = Substitute.For<IChannelId>();
            channelId.AsLongText().Returns(channelName);
            channelId.AsShortText().Returns(channelName);
            return channelId;
        }
    }

    public sealed class EmbeddedObservableChannel : IObservableChannel
    {
        private readonly EmbeddedChannel _channel;

        public EmbeddedObservableChannel(string channelName)
        {
            var channelId = channelName.ToChannelId();

            var observableServiceHandler = new ObservableServiceHandler();
            var embeddedChannel = new EmbeddedChannel(channelId, false, true, observableServiceHandler);
            _channel = embeddedChannel;
            MessageStream = observableServiceHandler.MessageStream;
        }

        public async Task SimulateReceivingMessagesAsync(params object[] messages)
        {
            await Task.Run(() => _channel.WriteInbound(messages)).ConfigureAwait(false);
            await MessageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
        }

        public IChannel Channel => _channel;
        public Task StartAsync() { return Task.CompletedTask; }
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }
    }
}
