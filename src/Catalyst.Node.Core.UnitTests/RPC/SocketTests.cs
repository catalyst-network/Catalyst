using System.IO;
using System.Net.Sockets;
using Autofac;
using Catalyst.Cli;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class SocketTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;
        private ICertificateStore _certificateStore;

        public SocketTests(ITestOutputHelper output) : base(output)
        {
            //Build configuration
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact]
        public void ServerConnectedToCorrectPort()
        {
            WriteLogsToFile = true;
            WriteLogsToTestOutput = true;
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);

            //Create container
            var container = ContainerBuilder.Build();
 
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();

                _rpcServer = container.Resolve<IRpcServer>();
                var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(), _rpcServer.Settings.Port);
                Assert.NotNull(client);
            }
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RpcServer_and_P2PServer_should_work_together()
        {
            WriteLogsToFile = true;
            WriteLogsToTestOutput = true;
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);

            //Create container
            var container = ContainerBuilder.Build();

            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();

                _rpcServer = container.Resolve<IRpcServer>();
                var client = new RpcClient(logger, _certificateStore);
                //var client = container.Resolve<IRpcClient>();
                client.Should().NotBeNull();
                _certificateStore = container.Resolve<ICertificateStore>();

                var peerSettings = new PeerSettings(_config) {Port = _rpcServer.Settings.Port + 1};
                var p2PMessenger = new P2PMessaging(peerSettings, _certificateStore, logger);
                p2PMessenger.Should().NotBeNull();

                //var connectedNodes = 
                

                //client.SendMessage(connectedNode)
            }
        }
    }
}