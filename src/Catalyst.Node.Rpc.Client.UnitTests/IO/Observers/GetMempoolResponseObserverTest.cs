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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    public sealed class GetMempoolResponseObserverTest : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        public static readonly List<object[]> QueryContents;

        private readonly IUserOutput _output;
        private GetMempoolResponseObserver _observer;

        static GetMempoolResponseObserverTest()
        {
            var memPoolData = CreateMemPoolData();

            QueryContents = new List<object[]>
            {
                new object[]
                {
                    memPoolData
                },
                new object[]
                {
                    new List<string>()
                }
            };
        }

        public GetMempoolResponseObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        private static IEnumerable<string> CreateMemPoolData()
        {
            var txLst = new List<TransactionBroadcast>
            {
                TransactionHelper.GetTransaction(234, "standardPubKey", "sign1"),
                TransactionHelper.GetTransaction(567, "standardPubKey", "sign2")
            };

            var txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();

            var mempoolList = new List<string>();

            foreach (var tx in txEncodedLst)
            {
                mempoolList.Add(Encoding.Default.GetString(tx));
            }

            return mempoolList;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]
        public async Task RpcClient_Can_Handle_GetMempoolResponse(IEnumerable<string> mempoolContent)
        {
            var txList = mempoolContent.ToList();

            var response = new DtoFactory().GetDto(
                new GetMempoolResponse
                {
                    Mempool = { txList }
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender_key"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                CorrelationId.GenerateCorrelationId()
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender_key").PeerId,
                    response.CorrelationId
                )
            );

            GetMempoolResponse messageStreamResponse = null;

            _observer = new GetMempoolResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);

            _observer.MessageResponseStream.Where(x => x.Message.GetType() == typeof(GetMempoolResponse)).SubscribeOn(NewThreadScheduler.Default).Subscribe((RpcClientMessageDto) =>
            {
                messageStreamResponse = (GetMempoolResponse)RpcClientMessageDto.Message;
            });

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            messageStreamResponse.Mempool.Count.Should().Be(txList.Count);
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
