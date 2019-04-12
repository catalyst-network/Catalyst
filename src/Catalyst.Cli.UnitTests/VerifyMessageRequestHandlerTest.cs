using System;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Catalyst.Cli.Handlers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Serilog.Extensions.Logging;

namespace Catalyst.Cli.UnitTests
{
    public sealed class VerifyMessageRequestHandlerTest : ConfigFileBasedTest, IDisposable
    {
        
        private readonly ILogger _logger;
        private readonly ILifetimeScope _scope;

        public VerifyMessageRequestHandlerTest(ITestOutputHelper output) : base(output)
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

        [Fact]
        public void RpcClient_Can_Handle_VerifyMessageResponse()
        {
            //Create a response object and set its return value
            var response = new VerifyMessageResponse()
            {
                IsSignedByKey = true
            }.ToAny();
            
            //Substitute for the context and the channel
            var fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            fakeContext.Channel.Returns(fakeChannel);
            
            //create a channel using the mock context and response
            var channeledAny = new ChanneledAny(fakeContext, response);

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
            var handler = new VerifyMessageResponseHandler(messageStream, _logger);

            //TODO: Check if the handler received the message
            //Another time delay is required so that the call to HandleMessage inside the VerifyMessageRequestHandler
            //is finished before we assert for the Received calls in the following statements.
            //Thread.Sleep(1000);

            //fakeContext.Channel.ReceivedWithAnyArgs(1).WriteAndFlushAsync(new VerifyMessageResponse().ToAny());

            //TODO: check if the handler can handle the received response
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) {return;}
            _scope?.Dispose();
        }
    }
}
