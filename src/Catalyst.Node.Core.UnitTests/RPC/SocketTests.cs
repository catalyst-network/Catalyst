using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Cli;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class SocketTests : ConfigFileBasedTest, IDisposable
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;
        private ICertificateStore _certificateStore;
        private RpcClient _rpcClient;
        private ILifetimeScope _scope;
        private ILogger _logger;

        public SocketTests(ITestOutputHelper output) : base(output)
        {
            //Build configuration
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ShellNodesConfigFile))
               .Build();

            AlterConfigurationToGetUniquePort();

            WriteLogsToFile = false;
            WriteLogsToTestOutput = false;

            ConfigureContainerBuilder(_config);
            var container = ContainerBuilder.Build();

            _scope = container.BeginLifetimeScope(_currentTestName);

            _logger = container.Resolve<ILogger>();
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider(_logger));

            _certificateStore = container.Resolve<ICertificateStore>();
            _rpcServer = container.Resolve<IRpcServer>();

        }

        [Fact]
        public void ServerConnectedToCorrectPort()
        {
            using (var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(),
                _rpcServer.Settings.Port))
            {
                client.Should().NotBeNull();
                client.Connected.Should().BeTrue();
            }
        }

        [Fact(Skip = "trying to exclude P2P from the tests for now")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_and_P2PServer_should_work_together()
        {
            _rpcClient = new RpcClient(_logger, _certificateStore);
            _rpcClient.Should().NotBeNull();

            var peerSettings = new PeerSettings(_config) {Port = _rpcServer.Settings.Port + 1000};
            var p2PMessenger = new P2PMessaging(peerSettings, _certificateStore, _logger);
            p2PMessenger.Should().NotBeNull();

            var shell = new Shell(_rpcClient, _config, _logger);
            var hasConnected = shell.OnCommand("connect", "node", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");
            node1.SocketClient.Channel.Active.Should().BeTrue();
            node1.SocketClient.Shutdown();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcClient_can_send_request_and_RpcServer_can_reply()
        {
            _rpcClient = new RpcClient(_logger, _certificateStore);
            _rpcClient.Should().NotBeNull();

            var shell = new Shell(_rpcClient, _config, _logger);
            var hasConnected = shell.OnCommand("connect", "node", "node1");
            hasConnected.Should().BeTrue();

            var node1 = shell.GetConnectedNode("node1");
            node1.Should().NotBeNull("we've just connected it");

            var serverObserver = new AnyMessageObserver(0, _logger);
            var clientObserver = new AnyMessageObserver(1, _logger);
            using (_rpcServer.MessageStream.Subscribe(serverObserver))
            using (_rpcClient.MessageStream.Subscribe(clientObserver))
            {
                var info = shell.OnGetCommand("get", "config", "node1");

                var tasks = new IChanneledMessageStreamer<Any>[] { _rpcClient, _rpcServer }
                   .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAny))
                   .ToArray();
                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(500));

                serverObserver.Received.Should().NotBeNull();
                serverObserver.Received.Payload.TypeUrl.Should().Be(GetInfoRequest.Descriptor.ShortenedFullName());

                clientObserver.Received.Should().NotBeNull();
                clientObserver.Received.Payload.TypeUrl.Should().Be(GetInfoResponse.Descriptor.ShortenedFullName());
            }
        
        }

        private void AlterConfigurationToGetUniquePort()
        {
            var serverSection = _config.GetSection("CatalystNodeConfiguration").GetSection("Rpc");
            var randomPort = int.Parse(serverSection.GetSection("Port").Value) +
                new Random(_currentTestName.GetHashCode()).Next(0, 500);

            serverSection.GetSection("Port").Value = randomPort.ToString();
            var clientSection = _config.GetSection("CatalystCliRpcNodes").GetSection("nodes");
            clientSection.GetChildren().ToList().ForEach(c => { c.GetSection("port").Value = randomPort.ToString(); });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) {return;}
            _scope?.Dispose();
            _rpcServer?.Dispose();
            _rpcClient?.Dispose();
        }
    }
}