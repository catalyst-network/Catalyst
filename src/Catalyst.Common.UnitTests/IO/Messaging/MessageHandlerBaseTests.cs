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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging
{
    public class VanillaMessageHandler : MessageHandlerBase<GetInfoResponse>
    {
        public IObserver<AnySigned> SubstituteObserver { get; }

        public VanillaMessageHandler(ILogger logger) : base(logger)
        {
            SubstituteObserver = Substitute.For<IObserver<AnySigned>>();
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            SubstituteObserver.OnNext(message.Payload);
        }

        public override void HandleError(Exception exception) { SubstituteObserver.OnError(exception); }
        public override void HandleCompleted() { SubstituteObserver.OnCompleted(); }
    }

    public class MessageHandlerBaseTests 

    {
        private readonly ILogger _logger;
        private readonly VanillaMessageHandler _handler;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly AnySigned[] _responseMessages;

        public MessageHandlerBaseTests()
        {
            _logger = Substitute.For<ILogger>();
            _handler = new VanillaMessageHandler(_logger);
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _responseMessages = Enumerable.Range(0, 10).Select(i =>
            {
                var message = new GetInfoResponse {Query = i.ToString()};
                return message.ToAnySigned(
                    PeerIdentifierHelper.GetPeerIdentifier(i.ToString()).PeerId,
                    Guid.NewGuid());
            }).ToArray();
        }

        [Fact]
        public async Task MessageHandler_should_subscribe_to_next_and_complete()
        {
            var completingStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _responseMessages);

            _handler.StartObserving(completingStream);
            await completingStream.LastAsync();

            _handler.SubstituteObserver.Received(10).OnNext(Arg.Any<AnySigned>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }

        [Fact]
        public void MessageHandler_should_subscribe_to_next_and_error()
        {
            var erroringStream = new Subject<IChanneledMessage<AnySigned>>();

            var simpleStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _responseMessages);

            _handler.StartObserving(erroringStream);

            foreach (var payload in _responseMessages)
            {
                if (payload.FromAnySigned<GetInfoResponse>().Query == 5.ToString())
                {
                    erroringStream.OnError(new ApplicationException("5 erred"));
                }

                erroringStream.OnNext(new ChanneledAnySigned(_fakeContext, payload));
            }

            _handler.SubstituteObserver.Received(5).OnNext(Arg.Any<AnySigned>());
            _handler.SubstituteObserver.Received(1).OnError(Arg.Is<Exception>(e => e is ApplicationException));
            _handler.SubstituteObserver.Received(0).OnCompleted();
        }

        [Fact]
        public async Task MessageHandler_should_not_receive_messages_of_the_wrong_type()
        {
            _responseMessages[3] = new PingResponse().ToAnySigned(
                _responseMessages[3].PeerId, 
                _responseMessages[3].CorrelationId.ToGuid());

            _responseMessages[7] = new PingRequest().ToAnySigned(
                _responseMessages[7].PeerId,
                _responseMessages[7].CorrelationId.ToGuid());

            var mixedTypesStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _responseMessages);

            _handler.StartObserving(mixedTypesStream);
            await mixedTypesStream.LastAsync();

            _handler.SubstituteObserver.Received(8).OnNext(Arg.Any<AnySigned>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }

        [Fact]
        public async Task MessageHandler_should_not_receive_null_or_untyped_messages()
        {
            _responseMessages[2].TypeUrl = "";
            _responseMessages[5] = NullObjects.AnySigned;
            _responseMessages[9] = null;

            var mixedTypesStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _responseMessages);

            _handler.StartObserving(mixedTypesStream);
            await mixedTypesStream.LastAsync();

            _handler.SubstituteObserver.Received(7).OnNext(Arg.Any<AnySigned>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }
    }
}
