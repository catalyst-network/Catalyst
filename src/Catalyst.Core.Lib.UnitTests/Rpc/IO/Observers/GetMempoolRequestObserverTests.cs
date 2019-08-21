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
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc.IO.Observers 
{
    public sealed class GetMempoolRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;

        public GetMempoolRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
        }
        
        public static IEnumerable<object[]> MempoolTransactions =>
            new List<object[]>
            {
                new object[] {CreateTestTransactions()},
                new object[] {new List<TransactionBroadcast>()}
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
        [MemberData(nameof(MempoolTransactions))]
        public async Task GetMempool_UsingFilledMempool_ShouldSendGetMempoolResponse(List<TransactionBroadcast> mempoolTransactions)
        {
            var mempool = Substitute.For<IMempool>();
            mempool.GetMemPoolContentAsTransactions().Returns(mempoolTransactions);

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
            
            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            
            var responseContent = sentResponseDto.FromIMessageDto().FromProtocolMessage<GetMempoolResponse>();

            responseContent.Transactions.Should().BeEquivalentTo(mempoolTransactions);
        }
    }
}
