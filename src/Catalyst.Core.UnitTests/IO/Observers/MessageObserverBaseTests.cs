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
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Observers
{
    public class VanillaMessageObserver : TestMessageObserver<GetInfoResponse>
    {
        public VanillaMessageObserver(ILogger logger) : base(logger) { }
    }

    public class ObservableBaseTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly VanillaMessageObserver _handler;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly ProtocolMessage[] _responseMessages;

        public ObservableBaseTests()
        {
            _testScheduler = new TestScheduler();
            _handler = new VanillaMessageObserver(Substitute.For<ILogger>());
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _responseMessages = Enumerable.Range(0, 10).Select(i =>
            {
                var message = new GetInfoResponse {Query = i.ToString()};
                return message.ToProtocolMessage(
                    PeerIdentifierHelper.GetPeerIdentifier(i.ToString()).PeerId,
                    CorrelationId.GenerateCorrelationId());
            }).ToArray();
        }

        [Fact]
#pragma warning disable 1998
        public async Task MessageHandler_should_subscribe_to_next_and_complete()
#pragma warning restore 1998
        {
            var completingStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _testScheduler, _responseMessages);

            _handler.StartObserving(completingStream);

            _testScheduler.Start();

            _handler.SubstituteObserver.Received(10).OnNext(Arg.Any<GetInfoResponse>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }

        [Fact]
#pragma warning disable 1998
        public async Task MessageHandler_should_subscribe_to_next_and_error()
#pragma warning restore 1998
        {
            var erroringStream = new ReplaySubject<IObserverDto<ProtocolMessage>>(10, _testScheduler);
            
            _handler.StartObserving(erroringStream);

            foreach (var payload in _responseMessages)
            {
                if (payload.FromProtocolMessage<GetInfoResponse>().Query == 5.ToString())
                {
                    erroringStream.OnError(new DataMisalignedException("5 erred"));
                }

                erroringStream.OnNext(new ObserverDto(_fakeContext, payload));
            }

            _testScheduler.Start();

            _handler.SubstituteObserver.Received(5).OnNext(Arg.Any<GetInfoResponse>());
            _handler.SubstituteObserver.Received(1).OnError(Arg.Is<Exception>(e => e is DataMisalignedException));
            _handler.SubstituteObserver.Received(0).OnCompleted();
        }

        [Fact]
#pragma warning disable 1998
        public async Task MessageHandler_should_not_receive_messages_of_the_wrong_type()
#pragma warning restore 1998
        {
            _responseMessages[3] = new PingResponse().ToProtocolMessage(
                _responseMessages[3].PeerId, 
                _responseMessages[3].CorrelationId.ToCorrelationId());

            _responseMessages[7] = new PingRequest().ToProtocolMessage(
                _responseMessages[7].PeerId,
                _responseMessages[7].CorrelationId.ToCorrelationId());

            var mixedTypesStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _testScheduler, _responseMessages);

            _handler.StartObserving(mixedTypesStream);

            _testScheduler.Start();

            _handler.SubstituteObserver.Received(8).OnNext(Arg.Any<GetInfoResponse>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }

        [Fact]
#pragma warning disable 1998
        public async Task MessageHandler_should_not_receive_null_or_untyped_messages()
#pragma warning restore 1998
        {
            _responseMessages[2].TypeUrl = "";
            _responseMessages[5] = NullObjects.ProtocolMessage;
            _responseMessages[9] = null;

            var mixedTypesStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, _testScheduler, _responseMessages);

            _handler.StartObserving(mixedTypesStream);

            _testScheduler.Start();

            _handler.SubstituteObserver.Received(7).OnNext(Arg.Any<GetInfoResponse>());
            _handler.SubstituteObserver.Received(0).OnError(Arg.Any<Exception>());
            _handler.SubstituteObserver.Received(1).OnCompleted();
        }
    }
}
