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
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables
{
    public sealed class VerifyMessageRequestObserverTest : ConfigFileBasedTest
    {
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public VerifyMessageRequestObserverTest(ITestOutputHelper output) : base(output)
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
        
        //[Theory(Skip = "Fails, fixing in #393")]
        [Theory]
        [InlineData("", "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b", "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a", true)]
        [InlineData("72", "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c0072", "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c", true)]
        [InlineData("af82", "6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40aaf82", "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025", true)]
        [InlineData("", "f5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b", "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a", false)]
        [InlineData("7255", "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c0072", "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c", false)]

        public void VerifyMessageRequest_UsingValidRequest_ShouldSendVerifyMessageResponse(string message, string signature, string publicKey, bool expectedResult)
        {
            var request = new DtoFactory().GetDto(
                new VerifyMessageRequest
                {
                    Message = RLP.EncodeElement(message.Trim('\"').ToBytesForRLPEncoding()).ToByteString(),
                    PublicKey = publicKey.HexToByteArray().ToByteString(),
                    Signature = signature.HexToByteArray().ToByteString()
                },
                PeerIdentifierHelper.GetPeerIdentifier("sender_key"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient_key")
            );
            
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, 
                request.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId)
            );
            
            var handler = new VerifyMessageRequestObserver(PeerIdentifierHelper.GetPeerIdentifier("sender"),
                _logger,
                _keySigner
            );
            
            handler.StartObserving(messageStream);
            
            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            
            var sentResponse = (ProtocolMessage) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(VerifyMessageResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromProtocolMessage<VerifyMessageResponse>();

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
