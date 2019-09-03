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
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Observers
{
    public sealed class SignMessageRequestObserverTests : ConfigFileBasedTest
    {
        private readonly TestScheduler _testScheduler;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly IKeySigner _keySigner;
        private readonly IChannelHandlerContext _fakeContext;

        public SignMessageRequestObserverTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile),
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet)),
            Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile)
        }, output)
        {
            _testScheduler = new TestScheduler();
            ContainerProvider.ConfigureContainerBuilder();
            SocketPortHelper.AlterConfigurationToGetUniquePort(ContainerProvider.ConfigurationRoot, CurrentTestName);

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _keySigner = ContainerProvider.Container.Resolve<IKeySigner>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        [Theory]
        [InlineData("Hello Catalyst")]
        [InlineData("")]
        [InlineData("Hello&?!1253Catalyst")]
#pragma warning disable 1998
        public async Task RpcServer_Can_Handle_SignMessageRequest(string message)
#pragma warning restore 1998
        {
            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var signMessageRequest = new SignMessageRequest
            {
                Message = message.ToUtf8ByteString(),
                SigningContext = new SigningContext()
            };
            var protocolMessage =
                signMessageRequest.ToProtocolMessage(sender.PeerId);

            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, protocolMessage);
            var handler =
                new SignMessageRequestObserver(sender, _logger, _keySigner);

            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            var signResponseMessage = sentResponseDto.Content.FromProtocolMessage<SignMessageResponse>();

            signResponseMessage.OriginalMessage.Should().Equal(message);
            signResponseMessage.Signature.Should().NotBeEmpty();
            signResponseMessage.PublicKey.Should().NotBeEmpty();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scope?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
