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

        public ServerUp(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact]
        public async Task Server_Up_And_Running()
        {
            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();

                var cliRPCServer = container.Resolve<ICLIRPCServer>();

                Task serverTask = cliRPCServer.RunServerAsync();
                
                Assert.NotNull(serverTask);
            }
        }

        public void ServerConnectedToCorrectPort()
        {
            
        }
    }
}