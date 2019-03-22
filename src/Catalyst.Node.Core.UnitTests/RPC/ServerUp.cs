using System.IO;
using System.Net.Sockets;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class ServerUp : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;

        public ServerUp(ITestOutputHelper output) : base(output)
        {
            //Build configuration
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            //Create ContainerBuilder based on the configuration
            ConfigureContainerBuilder(_config);

            //Create container
            var container = ContainerBuilder.Build();
            
            //Resolve the CLIRPCServer 
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();

                var cliRPCServer = container.Resolve<IRpcServer>();
                _rpcServer = cliRPCServer;
            }
        }

        [Fact]
        public void ServerConnectedToCorrectPort()
        {

            var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(), _rpcServer.Settings.Port);
            
            Assert.NotNull(client);
            
        }

        public void CanHandleGetNodeConfigCommand()
        {
            
        }
    }
}