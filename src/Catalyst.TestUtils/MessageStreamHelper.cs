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
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;

namespace Catalyst.TestUtils 
{
    public static class MessageStreamHelper
    {
        public static void SendToHandler(this ProtocolMessage messages, IChannelHandlerContext fakeContext, MessageObserverBase handler)
        {
            handler.OnNext(CreateChanneledMessage(fakeContext, messages));
        }

        public static IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithMessage(IChannelHandlerContext fakeContext, ProtocolMessage response)
        {   
            var channeledAny = new ObserverDto(fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }

        public static IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithMessages<T>(params T[] messages)
            where T : IMessage<T>, IMessage
        {
            var protoMessages = messages.Select(m =>
                m.ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId()));

            var context = Substitute.For<IChannelHandlerContext>();

            return CreateStreamWithMessages(context, protoMessages.ToArray());
        }

        public static IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithMessages(IChannelHandlerContext fakeContext, params ProtocolMessage[] responseMessages)
        {
            var stream = responseMessages
               .Select(message => new ObserverDto(fakeContext, message));

            var messageStream = stream.ToObservable();
            return messageStream;
        }

        private static ObserverDto CreateChanneledMessage(IChannelHandlerContext fakeContext, ProtocolMessage responseMessage)
        {
            return new ObserverDto(fakeContext, responseMessage);
        }

        public static IObservable<T> DelayAndSubscribeOnTaskPool<T>(this IObservable<T> messageStream, TimeSpan customDelay = default)
        {
            var delay = customDelay == default ? TimeSpan.FromMilliseconds(30) : customDelay;
            return messageStream.Delay(delay).SubscribeOn(TaskPoolScheduler.Default);
        }

        public static async Task<T> WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync<T>(this IObservable<T> messageStream, TimeSpan customDelay = default)
        {
            return await messageStream.DelayAndSubscribeOnTaskPool(customDelay).LastAsync();
        }

        public static async Task<T> WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync<T>(this IObservable<T> messageStream, int numberOfItemsToWaitFor = 1, TimeSpan customDelay = default)
        {
            return await messageStream.Take(numberOfItemsToWaitFor).DelayAndSubscribeOnTaskPool(customDelay).LastAsync();
        }
    }
}
