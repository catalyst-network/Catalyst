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
using System.Threading;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public sealed class SignMessageRequestHandlerTest : ConfigFileBasedTest, IDisposable
    {
        private const int MaxWaitInMs = 1000;
        private readonly IConfigurationRoot _config;

        private readonly ICertificateStore _certificateStore;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;

        private readonly IKeySigner _keySigner;

        public SignMessageRequestHandlerTest(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build(), CurrentTestName);

            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();

            _scope = container.BeginLifetimeScope(CurrentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory
               .AddProvider(new SerilogLoggerProvider(_logger));

            _certificateStore = container.Resolve<ICertificateStore>();
            _keySigner = container.Resolve<IKeySigner>();
        }
        
        [Fact] public void RpcServer_Can_Handle_SignMessageRequest()
        {
            //TODO: Check if we need to do all of this every time we need to use .ToAnySigned()
            var senderPeerId = PeerIdHelper.GetPeerId("sender");

            var peerIds = Enumerable.Range(0, 3).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"dude-{i}")).ToList();

            var correlationIds = Enumerable.Range(0, 3).Select(i => Guid.NewGuid()).ToList();

            //********************************************************************************
            
            //Substitute for the context and the channel
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            
            fakeContext.Channel.Returns(fakeChannel);

            //Matching the SignMessageRequestHandler
            //sign a message to get the signature and the public key
            const string message = "\"Hello Catalyst\"";

            //create a verify message request and populate with the data returned from the signed message
            var request = new SignMessageRequest()
            {
                Message = message.ToUtf8ByteString()
            };
            
            //create a channel using the mock context and request
            var channeledAny = new ChanneledAnySigned(fakeContext, request.ToAnySigned(peerIds[1].PeerId, correlationIds[1]));
            
            //convert the channel created into an IObservable 
            //the .ToObervable() Converts the array to an observable sequence which means we can use it as a message
            //stream for the handler
            //The DelaySubscription() is required otherwise the constructor of the base class will call the
            //HandleMessage before the VerifyMessageRequestHandler constructor is executed. This happens because
            //the base class will find a message in the stream. As a result the _keySigner object
            //will have not been instantiated before calling the HandleMessage.
            //This case will not happen in realtime but it is caused by the test environment.
            var signRequest = new[] {channeledAny}.ToObservable()
               .DelaySubscription(TimeSpan.FromMilliseconds(50));
            
            //pass the created observable sequence as the message stream to the VerifyMessageRequestHandler
            //Calling the constructor will call the 
            var handler = new SignMessageRequestHandler(signRequest, peerIds[1], _logger, _keySigner);
            
            //Another time delay is required so that the call to HandleMessage inside the VerifyMessageRequestHandler
            //is finished before we assert for the Received calls in the following statements.
            Thread.Sleep(10000);

            //Check the channel received 1 call to 
            var receivedCalls = fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);
            
            //Get the received response object and verify it is SignMessageResponse
            var sentResponse = (AnySigned) receivedCalls.Single().GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(SignMessageResponse.Descriptor.ShortenedFullName());
            
            //Get the contents of the response
            var responseContent = sentResponse.FromAnySigned<SignMessageResponse>();
            
            //Assert that the message sent in the response is the same message sent to sign
            responseContent.OriginalMessage.Should().Equal(message);
            
            //Assert that a signature was sent in the response
            responseContent.Signature.Should().NotBeEmpty();

            //Asset that a public key was sent in the response
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
