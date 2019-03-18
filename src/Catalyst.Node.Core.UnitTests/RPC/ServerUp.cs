using System;
using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;

using Autofac;

using Serilog;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    public class ServerUp : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        private ICLIRPCServer _rpcServer;

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

                var cliRPCServer = container.Resolve<ICLIRPCServer>();
                _rpcServer = cliRPCServer;
            }
        }

        [Fact]
        public void ServerConnectedToCorrectPort()
        {

            var client = new TcpClient(_rpcServer.Settings.BindAddress.ToString(), _rpcServer.Settings.Port);
            
            Assert.NotNull(client);
            
        }
    }
}