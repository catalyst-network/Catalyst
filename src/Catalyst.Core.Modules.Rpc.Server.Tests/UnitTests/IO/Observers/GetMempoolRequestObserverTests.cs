#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Mempool;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests.IO.Observers
{
    public sealed class GetMempoolRequestObserverTests
    {
        private ILogger _logger;
        private IChannelHandlerContext _fakeContext;
        private TestMapperProvider _mapperProvider;

        [SetUp]
        public void Init()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort));
            _mapperProvider = new TestMapperProvider();
        }

        public static IEnumerable<object[]> MempoolTransactions =>
            new List<object[]>
            {
                new object[] {CreateTestTransactions()},
                new object[] {new List<PublicEntryDao>()}
            };

        private static List<PublicEntryDao> CreateTestTransactions()
        {
            TestMapperProvider mapperProvider = new();
            var txLst = new List<TransactionBroadcast>
            {
                TransactionHelper.GetPublicTransaction(234, "standardPubKey", "sign1"),
                TransactionHelper.GetPublicTransaction(567, "standardPubKey", "sign2")
            }.Select(x => x.PublicEntry.ToDao<PublicEntry, PublicEntryDao>(mapperProvider)).ToList();

            return txLst;
        }

        [TestCaseSource(nameof(MempoolTransactions))]
        public void GetMempool_UsingFilledMempool_ShouldSendGetMempoolResponse(List<PublicEntryDao> mempoolTransactions)
        {
            TestScheduler testScheduler = new();
            var mempool = Substitute.For<IMempool<PublicEntryDao>>();
            mempool.Service.GetAll().Returns(mempoolTransactions);

            var protocolMessage = new GetMempoolRequest().ToProtocolMessage(MultiAddressHelper.GetAddress("sender_key"));

            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler, protocolMessage);

            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            GetMempoolRequestObserver handler = new(peerSettings, mempool, _mapperProvider, _logger);

            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();

            var responseContent = sentResponseDto.Content.FromProtocolMessage<GetMempoolResponse>();

            responseContent.Transactions.Select(x => x.ToDao<PublicEntry, PublicEntryDao>(_mapperProvider)).Should()
               .BeEquivalentTo(mempoolTransactions);
        }
    }
}
