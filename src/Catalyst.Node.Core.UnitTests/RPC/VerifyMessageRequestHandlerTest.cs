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
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Multiformats.Base;
using Nethereum.RLP;
using NSec.Cryptography;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public sealed class VerifyMessageRequestHandlerTest : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public VerifyMessageRequestHandlerTest(ITestOutputHelper output) : base(output)
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
        [InlineData("hello", "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIAw", "zGfHq2tTVk9z4eXgyUwcss5uApFrvVdAjf395XdQt2wbY8drESxbLQSHrbSx2", true)]
        [InlineData("Different Message", "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIAw", "zGfHq2tTVk9z4eXgyUwcss5uApFrvVdAjf395XdQt2wbY8drESxbLQSHrbSx2", false)]
        [InlineData("hello", "any signature", "zGfHq2tTVk9z4eXgyUwcss5uApFrvVdAjf395XdQt2wbY8drESxbLQSHrbSx2", false)]
        [InlineData("hello", "mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIAw", "any public key", false)]
        [InlineData("hello", "any signature", "any public key", false)]
        [InlineData("", "", "", false)]
        public void VerifyMessageRequest_UsingValidRequest_ShouldSendVerifyMessageResponse(string message, string signature, string publicKey, bool expectedResult)
        {   
            var request = new VerifyMessageRequest()
            {
                Message = RLP.EncodeElement(message.Trim('\"').ToBytesForRLPEncoding()).ToByteString(),
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Signature = signature.ToBytesForRLPEncoding().ToByteString()
            }.ToAnySigned(PeerIdHelper.GetPeerId("sender"), Guid.NewGuid());
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, request);
            
            var handler = new VerifyMessageRequestHandler(PeerIdentifierHelper.GetPeerIdentifier($"sender"), _logger, _keySigner);
            handler.StartObserving(messageStream);
            
            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);
            
            var sentResponse = (AnySigned) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(VerifyMessageResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<VerifyMessageResponse>();

            responseContent.IsSignedByKey.Should().Be(expectedResult);
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
