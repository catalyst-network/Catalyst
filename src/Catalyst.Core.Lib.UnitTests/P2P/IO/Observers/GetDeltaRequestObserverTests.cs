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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Multiformats.Base;
using Multiformats.Hash;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Observers
{
    public sealed class GetDeltaRequestObserverTests
    {
        private readonly IDeltaCache _deltaCache;
        private readonly GetDeltaRequestObserver _observer;
        private readonly IChannelHandlerContext _fakeContext;

        public GetDeltaRequestObserverTests()
        {
            var logger = Substitute.For<ILogger>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("responder");
            _deltaCache = Substitute.For<IDeltaCache>();
            _observer = new GetDeltaRequestObserver(_deltaCache, peerIdentifier, logger);
            _fakeContext = Substitute.For<IChannelHandlerContext>();
        }

        [Fact]
        public async Task GetDeltaRequestObserver_Should_Not_Hit_The_Cache_On_Invalid_Hash()
        {
            var invalidHash = "abcd";
            var invalidHashBytes = Encoding.UTF8.GetBytes(invalidHash);
            CreateAndExpectDeltaFromCache(invalidHash);

            var observable = CreateStreamWithDeltaRequest(invalidHashBytes);

            _observer.StartObserving(observable);

            await observable.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            _deltaCache.DidNotReceiveWithAnyArgs().TryGetConfirmedDelta(default, out _);
            await _fakeContext.Channel.DidNotReceiveWithAnyArgs().WriteAndFlushAsync(default);
        }

        [Fact]
        public async Task GetDeltaRequestObserver_Should_Send_Response_When_Delta_Found_In_Cache()
        {
            var multiHash = GetMultiHash("abcd");

            var delta = CreateAndExpectDeltaFromCache(multiHash.AsBase32Address());

            var observable = CreateStreamWithDeltaRequest(multiHash);

            _observer.StartObserving(observable);

            await observable.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            _deltaCache.Received(1).TryGetConfirmedDelta(Arg.Is<string>(
                s => s.Equals(multiHash.AsBase32Address())), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm => 
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta.PreviousDeltaDfsHash == delta.PreviousDeltaDfsHash));
        }

        [Fact]
        public async Task GetDeltaRequestObserver_Should_Send_Response_With_Null_Content_If_Not_Retrieved_In_Cache()
        {
            var multiHash = GetMultiHash("defg");

            var observable = CreateStreamWithDeltaRequest(multiHash);

            _observer.StartObserving(observable);

            await observable.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            _deltaCache.Received(1).TryGetConfirmedDelta(Arg.Is<string>(
                s => s.Equals(multiHash.AsBase32Address())), out Arg.Any<Delta>());

            await _fakeContext.Channel.ReceivedWithAnyArgs(1)
               .WriteAndFlushAsync(Arg.Is<IMessageDto<ProtocolMessage>>(pm =>
                    pm.Content.FromProtocolMessage<GetDeltaResponse>().Delta == null));
        }

        private static Multihash GetMultiHash(string content)
        {
            var multiHash = Encoding.UTF8.GetBytes(content).ComputeMultihash(Common.Config.Constants.HashAlgorithm);
            return multiHash;
        }

        private IObservable<IObserverDto<ProtocolMessage>> CreateStreamWithDeltaRequest(byte[] hash)
        {
            var deltaRequest = new GetDeltaRequest {DeltaDfsHash = hash.ToByteString()};

            var message = deltaRequest.ToProtocolMessage(PeerIdHelper.GetPeerId("sender"));

            var observable = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, message);
            return observable;
        }

        private Delta CreateAndExpectDeltaFromCache(string hash)
        {
            var delta = DeltaHelper.GetDelta();

            _deltaCache.TryGetConfirmedDelta(Arg.Is(hash), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = delta;
                    return true;
                });
            return delta;
        }
    }
}

