using System.IO;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P;
using NSubstitute;
using SharpRepository.Repository;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PeerDiscoveryUnitTest : ConfigFileBasedTest
    {
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly IDns _dns;
        private readonly IRepository<IPeer> _peerRepository;
        private readonly IConfigurationRoot _config;
        private readonly ILogger _logger;

        public PeerDiscoveryUnitTest(ITestOutputHelper output) : base(output)
        {
            _dns = Substitute.For<IDns>();
            _peerRepository = Substitute.For<IRepository<IPeer>>();
            _logger = Substitute.For<ILogger>();

            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            _peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);
        }

        [Fact]
        public async Task ResolvesIPeerCorrectly()
        {
            WriteLogsToFile = true;
            WriteLogsToTestOutput = true;

            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var peerDiscovery = container.Resolve<IPeerDiscovery>();
                Assert.NotNull(peerDiscovery);
            }
        }

        [Fact]
        public async Task CanParseDnsNodesFromConfig()
        {
            _peerDiscovery.ParseDnsServersFromConfig(_config);
            _peerDiscovery.SeedNodes.Should().NotBeNullOrEmpty();
        }
    }
}
