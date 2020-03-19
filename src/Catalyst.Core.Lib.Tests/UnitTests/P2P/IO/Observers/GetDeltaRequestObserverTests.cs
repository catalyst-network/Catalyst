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
using Autofac;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Lib.P2P;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class GetDeltaRequestObserverTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly IDeltaCache _deltaCache;
        private readonly GetDeltaRequestObserver _observer;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IHashProvider _hashProvider;

        public GetDeltaRequestObserverTests()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<HashingModule>();
            var container = builder.Build();
            container.BeginLifetimeScope();

            _hashProvider = container.Resolve<IHashProvider>();

            _testScheduler = new TestScheduler();
            var logger = Substitute.For<ILogger>();
            var peerIdentifier = PeerIdHelper.GetPeerId("responder");
            var peerSettings = peerIdentifier.ToSubstitutedPeerSettings();
            _deltaCache = Substitute.For<IDeltaCache>();
            _observer = new GetDeltaRequestObserver(_deltaCache, peerSettings, logger);
            _fakeContext = Substitute.For<IChannelHandlerContext>();
        }

        [Test]
        public async Task GetDeltaRequestObserver_Should_Send_Response_When_Delta_Found_In_Cache()
        {
            var cid = _hashProvider.ComputeUtf8MultiHash("abcd").ToCid();
            var delta = CreateAndExpectDeltaFromCache(cid);
            var observable = CreateStreamWithDeltaRequest(cid);

            _observer.StartObserving(observable);

            _testScheduler.Start();

            _deltaCache.Received(1).TryGetOrAddConfirmedDelta(Arg.Is<Cid>(
                s => s.Equals(cid)), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm =>
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta.PreviousDeltaDfsHash ==
                    delta.PreviousDeltaDfsHash));
        }

        [Test]
        public async Task GetDeltaRequestObserver_Should_Send_Response_With_Null_Content_If_Not_Retrieved_In_Cache()
        {
            var cid = _hashProvider.ComputeUtf8MultiHash("defg").ToCid();

            var observable = CreateStreamWithDeltaRequest(cid);

            _observer.StartObserving(observable);

            _testScheduler.Start();

            _deltaCache.Received(1).TryGetOrAddConfirmedDelta(Arg.Is<Cid>(
                s => s.Equals(cid)), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm =>
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta == null));
        }

        private IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithDeltaRequest(Cid cid)
        {
            var deltaRequest = new GetDeltaRequest {DeltaDfsHash = cid.ToArray().ToByteString()};

            var message = deltaRequest.ToProtocolMessage(PeerIdHelper.GetPeerId("sender"));

            var observable = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, message);
            return observable;
        }

        private Delta CreateAndExpectDeltaFromCache(Cid cid)
        {
            var delta = DeltaHelper.GetDelta(_hashProvider);

            _deltaCache.TryGetOrAddConfirmedDelta(Arg.Is(cid), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = delta;
                    return true;
                });
            return delta;
        }
    }
}
