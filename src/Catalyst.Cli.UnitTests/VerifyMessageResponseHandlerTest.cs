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
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Catalyst.Cli.Handlers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Serilog.Extensions.Logging;

namespace Catalyst.Cli.UnitTests
{
    public sealed class VerifyMessageResponseHandlerTest : ConfigFileBasedTest, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILifetimeScope _scope;

        public VerifyMessageResponseHandlerTest(ITestOutputHelper output) : base(output)
        {
            var targetConfigFolder = FileSystem.GetCatalystHomeDir().FullName;

            new CliConfigCopier().RunConfigStartUp(targetConfigFolder, Network.Dev);

            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(targetConfigFolder, Constants.ShellNodesConfigFile))
               .Build();
            
            ConfigureContainerBuilder(config);
            
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            var serviceCollection = new ServiceCollection();
            var container = ContainerBuilder.Build();
            _scope = container.BeginLifetimeScope(CurrentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));
        }

        [Fact] public void RpcClient_Can_Handle_VerifyMessageResponse()
        {   
            //Create a response object and set its return value
            var response = new VerifyMessageResponse()
            {
                IsSignedByKey = true
            }.ToAnySigned(PeerIdHelper.GetPeerId("sender"), Guid.NewGuid());
            
            //Substitute for the context and the channel
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            fakeContext.Channel.Returns(fakeChannel);
            
            //create a channel using the mock context and response
            var channeledAny = new ChanneledAnySigned(fakeContext, response);

            //convert the channel created into an IObservable 
            //the .ToObervable() Converts the array to an observable sequence which means we can use it as a message
            //stream for the handler
            //The DelaySubscription() is required otherwise the constructor of the base class will call the
            //HandleMessage before the VerifyMessageRequestHandler constructor is executed. This happens because
            //the base class will find a message in the stream. As a result the _keySigner object
            //will have not been instantiated before calling the HandleMessage.
            //This case will not happen in realtime but it is caused by the test environment.
            var messageStream = new[] {channeledAny}.ToObservable()
               .DelaySubscription(TimeSpan.FromMilliseconds(50));

            //Create a VerifyMessageResponseHandler instance and subscribe it to the created message stream
            //Send the response on the message stream
            var handler = new VerifyMessageResponseHandler(_logger);
            handler.StartObserving(messageStream);

            //Some check needs to be added to verify the handler received the message
            //Another time delay is required so that the call to HandleMessage inside the VerifyMessageRequestHandler
            //is finished before we assert for the Received calls in the following statements.
            Thread.Sleep(500);
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
