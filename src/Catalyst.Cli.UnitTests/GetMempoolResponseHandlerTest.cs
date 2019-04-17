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
using System.Reactive.Linq;
using System.Text;
using Catalyst.Cli.Handlers;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using Google.Protobuf.Collections;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Cli.UnitTests 
{
    public sealed class GetMempoolResponseHandlerTest : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        public static readonly List<object[]> QueryContents;
        
        private readonly IUserOutput _output;
        private GetMempoolResponseHandler _handler;

        static GetMempoolResponseHandlerTest()
        {
            var memPoolData = CreateMemPoolData();
            
            QueryContents = new List<object[]>()
            {
                new object[] {memPoolData},
                new object[] {new MapField<string, string>()},
            };
        }
        
        public GetMempoolResponseHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }
        
        private IObservable<ChanneledAnySigned> CreateStreamWithMessage(AnySigned response)
        {
            var channeledAny = new ChanneledAnySigned(_fakeContext, response);
            var messageStream = new[] {channeledAny}.ToObservable();
            return messageStream;
        }

        public static MapField<string, string> CreateMemPoolData()
        {
            var txLst = new List<Transaction>
            {
                TransactionHelper.GetTransaction(234, "standardPubKey", "sign1"),
                TransactionHelper.GetTransaction(567, "standardPubKey", "sign2")
            };  
            
            var txEncodedLst = txLst.Select(tx => tx.ToString().ToBytesForRLPEncoding()).ToList();
            
            var memPoolMap = new MapField<string, string>();

            for (var i = 0; i < txEncodedLst.Count; i++)
            {
                var sb = new StringBuilder("{");
                foreach (var b in txEncodedLst[i])
                {
                    sb.Append(b);
                }

                sb.Append("}");

                memPoolMap.Add(i.ToString(), sb.ToString());
            }

            return memPoolMap;
        }

        [Theory]
        [MemberData(nameof(QueryContents))]  
        public void RpcClient_Can_Handle_GetMempoolResponse(MapField<string, string> memPoolMap)
        {   
            var response = new GetMempoolResponse()
            {
                Info = {memPoolMap}
            }.ToAnySigned(PeerIdHelper.GetPeerId("sender"), Guid.NewGuid());

            var messageStream = CreateStreamWithMessage(response);
            
            _handler = new GetMempoolResponseHandler(_output, _logger);
            _handler.StartObserving(messageStream);
            
            _output.Received(memPoolMap.Count).WriteLine(Arg.Any<string>());
        }
        
        public void Dispose()
        {
            _handler?.Dispose();
        }
    }
}
