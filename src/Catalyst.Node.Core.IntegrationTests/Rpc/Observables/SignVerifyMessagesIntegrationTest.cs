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

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.IntegrationTests.Rpc.Observables
{
    public class SignVerifyMessagesIntegrationTest : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IChannelHandlerContext _fakeContext2;
        
        public SignVerifyMessagesIntegrationTest(ITestOutputHelper output) : base(output)
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
            _fakeContext2 = Substitute.For<IChannelHandlerContext>();
            var fakeChannel2 = Substitute.For<IChannel>();
            _fakeContext2.Channel.Returns(fakeChannel2);
        }

        [Theory]
        [InlineData("Hello Catalyst")]
        [InlineData("")]
        [InlineData("Hello&?!1253Catalyst")]
        public async Task SignMessageResponse_Is_Verified_By_VerifyMessageResponse(string message)
        {
            var messageFactory = new DtoFactory();
            var encodedMessage = RLP.EncodeElement(message.ToBytesForRLPEncoding()).ToByteString();
            
            var request = messageFactory.GetDto(
                new SignMessageRequest
                {
                    Message = encodedMessage
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender_key"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key")
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, 
                request.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId)
            );

            var handler = new SignMessageRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"), _logger, _keySigner);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            
            var sentResponseDto = (IMessageDto<SignMessageResponse>) receivedCalls.Single().GetArguments().Single();

            var signResponseMessage = sentResponseDto.FromIMessageDto();

            
            
            var verifyRequest = messageFactory.GetDto(
                new VerifyMessageRequest
                {
                    Message = signResponseMessage.OriginalMessage,
                    PublicKey = signResponseMessage.PublicKey,
                    Signature = signResponseMessage.Signature
                },
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key")
            );
            
            var verifyMessageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext2, 
                verifyRequest.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId)
            );
            
            var verifyMessageRequestObserver = new VerifyMessageRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"),
                _logger,
                _keySigner
            );

            verifyMessageRequestObserver.StartObserving(verifyMessageStream);

            await verifyMessageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedVerifyCalls = _fakeContext2.Channel.ReceivedCalls().ToList();

            var sentResponseDto2 = (IMessageDto<VerifyMessageResponse>) receivedVerifyCalls.Single().GetArguments().Single();

            var verifyResponseMessage = sentResponseDto2.FromIMessageDto();

            verifyResponseMessage.IsSignedByKey.Should().Be(true);
        }
    }
}
