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
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.Observables
{
    public sealed class GetInfoResponseObserverTest : IDisposable
    {
        private readonly ILogger _logger;
        private GetInfoResponseObserver _requestObserver;

        private static readonly List<object[]> QueryContents;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IUserOutput _output;

        static GetInfoResponseObserverTest()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build();

            var configurationRoot = config.GetChildren().ToList().First();

            var query = JsonConvert.SerializeObject(configurationRoot.AsEnumerable(),
                Formatting.Indented);

            QueryContents = new List<object[]>
            {
                new object[] {query},
                new object[] {""}
            };
        }

        public GetInfoResponseObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        private IObservable<ProtocolMessageDto> CreateStreamWithMessage(ProtocolMessage response)
        {
            var channeledAny = new ProtocolMessageDto(_fakeContext, response);
            var messageStream = new[]
            {
                channeledAny
            }.ToObservable();
            return messageStream;
        }

        [Theory(Skip = "This doesn't make sense")]
        [MemberData(nameof(QueryContents))]
        public void RpcClient_Can_Handle_GetInfoResponse(string query)
        {
            var response = new ProtocolProtocolMessageFactory().GetMessage(new MessageDto(
                    new GetInfoResponse
                    {
                        Query = query
                    },
                    MessageTypes.Response,
                    PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")
                ),
                Guid.NewGuid());

            var messageStream = CreateStreamWithMessage(response);

            _requestObserver = new GetInfoResponseObserver(_output, _logger);
            _requestObserver.StartObserving(messageStream);

            _output.Received(1).WriteLine(query);
        }

        public void Dispose()
        {
            _requestObserver?.Dispose();
        }
    }
}
