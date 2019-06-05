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
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.UnitTests.TestUtils 
{
    public static class MessageStreamHelper
    {
        public static void SendToHandler<T>(this ProtocolMessage[] messages, IChannelHandlerContext fakeContext, MessageHandlerBase<T> handler) where T : IMessage
        {
            CreateChanneledMessage(fakeContext, messages).ForEach(handler.HandleMessage);
        }

        public static void SendToHandler<T>(this ProtocolMessage messages, IChannelHandlerContext fakeContext, MessageHandlerBase<T> handler) where T : IMessage
        {
            handler.HandleMessage(CreateChanneledMessage(fakeContext, messages));
        }

        public static IObservable<IChanneledMessage<ProtocolMessage>> CreateStreamWithMessage(IChannelHandlerContext fakeContext, ProtocolMessage response)
        {   
            var channeledAny = new ProtocolMessageDto(fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }

        public static IObservable<IChanneledMessage<ProtocolMessage>> CreateStreamWithMessages(IChannelHandlerContext fakeContext, params ProtocolMessage[] responseMessages)
        {
            var stream = responseMessages
               .Select(message => new ProtocolMessageDto(fakeContext, message));

            var messageStream = stream.ToObservable();
            return messageStream;
        }

        private static ProtocolMessageDto CreateChanneledMessage(IChannelHandlerContext fakeContext, ProtocolMessage responseMessage)
        {
            return new ProtocolMessageDto(fakeContext, responseMessage);
        }

        private static List<ProtocolMessageDto> CreateChanneledMessage(IChannelHandlerContext fakeContext, params ProtocolMessage[] responseMessages)
        {
            var stream = new List<ProtocolMessageDto>();

            foreach (var message in responseMessages)
            {
                var channeledAny = new ProtocolMessageDto(fakeContext, message);
                stream.Add(channeledAny);
            }

            return stream;
        }
    }
}
