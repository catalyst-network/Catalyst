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
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Ipfs;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;
using Base32 = SimpleBase.Base32;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Observers
{
    public sealed class GetDeltaRequestObserverTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly IDeltaCache _deltaCache;
        private readonly GetDeltaRequestObserver _observer;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IContainer _container;
        private readonly IHashProvider _hashProvider;

        public GetDeltaRequestObserverTests()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<HashingModule>();
            _container = builder.Build();
            _container.BeginLifetimeScope();

            _hashProvider = _container.Resolve<IHashProvider>();

            _testScheduler = new TestScheduler();
            var logger = Substitute.For<ILogger>();
            var peerIdentifier = PeerIdHelper.GetPeerId("responder");
            var peerSettings = peerIdentifier.ToSubstitutedPeerSettings();
            _deltaCache = Substitute.For<IDeltaCache>();
            _observer = new GetDeltaRequestObserver(_hashProvider, _deltaCache, peerSettings, logger);
            _fakeContext = Substitute.For<IChannelHandlerContext>();
        }

        //[Fact]
        //public async Task GetDeltaRequestObserver_Should_Not_Hit_The_Cache_On_Invalid_Hash()
        //{
        //    var invalidHash = "abcd";
        //    var invalidHashBytes = Encoding.UTF8.GetBytes(invalidHash);
        //    var invalidMultiHash = _hashProvider.ComputeMultiHash(invalidHashBytes);
        //    CreateAndExpectDeltaFromCache(invalidMultiHash);

        //    var observable = CreateStreamWithDeltaRequest(invalidMultiHash);

        //    _observer.StartObserving(observable);

        //    _testScheduler.Start();

        //    _deltaCache.DidNotReceiveWithAnyArgs().TryGetOrAddConfirmedDelta(default, out _);
        //    await _fakeContext.Channel.DidNotReceiveWithAnyArgs().WriteAndFlushAsync(default);
        //}

        [Fact]
        public async Task GetDeltaRequestObserver_Should_Send_Response_When_Delta_Found_In_Cache()
        {
            var multiHash = _hashProvider.ComputeUtf8MultiHash("abcd");
            var multiHashBase32 = multiHash.ToBase32();

            var delta = CreateAndExpectDeltaFromCache(multiHash);

            var observable = CreateStreamWithDeltaRequest(multiHash);

            _observer.StartObserving(observable);

            _testScheduler.Start();

            _deltaCache.Received(1).TryGetOrAddConfirmedDelta(Arg.Is<MultiHash>(
                s => s.ToBase32().Equals(multiHashBase32)), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm =>
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta.PreviousDeltaDfsHash ==
                    delta.PreviousDeltaDfsHash));
        }

        [Fact]
        public async Task GetDeltaRequestObserver_Should_Send_Response_With_Null_Content_If_Not_Retrieved_In_Cache()
        {
            var multiHash = _hashProvider.ComputeUtf8MultiHash("defg");

            var observable = CreateStreamWithDeltaRequest(multiHash);

            _observer.StartObserving(observable);

            _testScheduler.Start();

            _deltaCache.Received(1).TryGetOrAddConfirmedDelta(Arg.Is<MultiHash>(
                s => s.Equals(multiHash)), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm =>
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta == null));
        }

        private IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithDeltaRequest(MultiHash hash)
        {
            var deltaRequest = new GetDeltaRequest {DeltaDfsHash = hash.ToArray().ToByteString()};

            var message = deltaRequest.ToProtocolMessage(PeerIdHelper.GetPeerId("sender"));

            var observable = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, message);
            return observable;
        }

        private Delta CreateAndExpectDeltaFromCache(MultiHash hash)
        {
            var delta = DeltaHelper.GetDelta(_hashProvider);

            _deltaCache.TryGetOrAddConfirmedDelta(Arg.Is(hash), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = delta;
                    return true;
                });
            return delta;
        }
    }
}
