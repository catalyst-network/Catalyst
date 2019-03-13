using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P.Messaging
{
    public class PeersCanChat : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        public PeersCanChat(ITestOutputHelper output) : base(output)
        {
            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
        }

        [Fact(Skip = "not ready")]
        public async Task Peers_Can_Chat()
        {
            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var certificateStore = container.Resolve<ICertificateStore>();

                var peerSettings = Enumerable.Range(0, 3).Select(
                    i => new PeerSettings(_config) { Port = 40100 + i }).ToList();

                var peers = peerSettings
                   .Select(s => new P2PMessaging(s, certificateStore, logger))
                   .ToList();

                await peers[0].BroadcastMessageAsync(message: peers[1].Identifier.ToString());
            }
        }
    }
}
