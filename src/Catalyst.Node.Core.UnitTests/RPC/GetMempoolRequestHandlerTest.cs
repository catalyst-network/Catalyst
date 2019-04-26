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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.RPC 
{
    public sealed class GetMempoolRequestHandlerTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;

        public GetMempoolRequestHandlerTest()
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
                new object[] {new List<Transaction>(), 0},
            };

        private static List<Transaction> CreateTestTransactions()
        {
            var txLst = new List<Transaction>
            {
                TransactionHelper.GetTransaction(234, "standardPubKey", "sign1"),
                TransactionHelper.GetTransaction(567, "standardPubKey", "sign2")
            };

            return txLst;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public void GetMempool_UsingFilledMempool_ShouldSendGetMempoolResponse(List<Transaction> txLst, int expectedTxs)
        {
            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContentEncoded().Returns(x =>
                {
                    var txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();
                    return txEncodedLst;
                }
            );

            var request = new RpcMessageFactory<GetMempoolRequest, RpcMessages>().GetMessage(
                new MessageDto<GetMempoolRequest, RpcMessages>(
                    RpcMessages.GetMempoolRequest,
                    new GetMempoolRequest(),
                    PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender_key"))
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var subbedCache = Substitute.For<IMessageCorrelationCache>();
            var handler = new GetMempoolRequestHandler(PeerIdentifierHelper.GetPeerIdentifier("sender"), mempool, subbedCache, _logger);
            handler.StartObserving(messageStream);
            
            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(2);
            
            var sentResponse = (AnySigned) receivedCalls[1].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetMempoolResponse.Descriptor.ShortenedFullName());
            var responseContent = sentResponse.FromAnySigned<GetMempoolResponse>();

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
