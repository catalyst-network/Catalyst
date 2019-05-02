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
using System.IO;
using System.Linq;
using System.Text;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public sealed class SignMessageRequestHandlerTest : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;

        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public SignMessageRequestHandlerTest(ITestOutputHelper output) : base(output)
        {
            var config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), CurrentTestName);

            ConfigureContainerBuilder(config);

            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(CurrentTestName);
            
            _keySigner = container.Resolve<IKeySigner>();
            
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }
        
        [Theory]
        [InlineData("Hello Catalyst")]
        [InlineData("")]
        [InlineData("Hello&?!1253Catalyst")]
        public void RpcServer_Can_Handle_SignMessageRequest(string message)
        {            
            var request = new RpcMessageFactory<SignMessageRequest, RpcMessages>().GetMessage(
                new MessageDto<SignMessageRequest, RpcMessages>(
                    RpcMessages.SignMessageRequest,
                    new SignMessageRequest
                    {
                        Message = ByteString.CopyFrom(message.Trim('\"'), Encoding.UTF8)
                    }, 
                    PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender_key"))
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            var subbedCache = Substitute.For<IMessageCorrelationCache>();
            var handler = new SignMessageRequestHandler(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, _keySigner, subbedCache);
            handler.StartObserving(messageStream);
             
            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            
            var sentResponse = (AnySigned) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(SignMessageResponse.Descriptor.ShortenedFullName());
            
            var responseContent = sentResponse.FromAnySigned<SignMessageResponse>();
            
            responseContent.OriginalMessage.Should().Equal(message);
            
            responseContent.Signature.Should().NotBeEmpty();

            responseContent.PublicKey.Should().NotBeEmpty();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }
            
            _scope?.Dispose();
        }
    }
}
