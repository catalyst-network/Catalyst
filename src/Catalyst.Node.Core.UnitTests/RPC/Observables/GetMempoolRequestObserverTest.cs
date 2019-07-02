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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.RPC.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables 
{
    public sealed class GetMempoolRequestObserverTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;

        public GetMempoolRequestObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
        }
        
        public static IEnumerable<object[]> QueryContents =>
            new List<object[]>
            {
                new object[] {CreateTestTransactions(), 2},
                new object[] {new List<TransactionBroadcast>(), 0}
            };

        private static List<TransactionBroadcast> CreateTestTransactions()
        {
            var txLst = new List<TransactionBroadcast>
            {
                TransactionHelper.GetTransaction(234, "standardPubKey", "sign1"),
                TransactionHelper.GetTransaction(567, "standardPubKey", "sign2")
            };

            return txLst;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public async Task GetMempool_UsingFilledMempool_ShouldSendGetMempoolResponse(List<TransactionBroadcast> txLst, int expectedTxs)
        {
            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContentEncoded().Returns(x =>
                {
                    var txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();
                    return txEncodedLst;
                }
            );

            var request = new DtoFactory().GetDto(
                new GetMempoolRequest(),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                PeerIdentifierHelper.GetPeerIdentifier("sender_key")
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId));
            var handler = new GetMempoolRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"), mempool, _logger);
            
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            
            var sentResponseDto = (IMessageDto<GetMempoolResponse>) receivedCalls.Single().GetArguments().Single();
            sentResponseDto.Content.GetType().Should().BeAssignableTo<GetMempoolResponse>();

            var responseContent = sentResponseDto.FromIMessageDto();
            
            if (expectedTxs == 0)
            {
                responseContent.Mempool.Should().BeEmpty();
                return;
            }

            responseContent.Mempool.Should().NotBeEmpty();
            responseContent.Mempool.Count.Should().Be(expectedTxs);
            
            var mempoolContent = responseContent.Mempool.ToList();

            for (var i = 0; i < mempoolContent.Count; i++)
            {
                var tx = mempoolContent[i];

                tx.Should().NotBeEmpty().And.ContainEquivalentOf(txLst[i].Signature.ToString());
            }
        }
    }
}
