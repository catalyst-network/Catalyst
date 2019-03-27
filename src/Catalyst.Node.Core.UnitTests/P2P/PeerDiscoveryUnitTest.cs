using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P;
using NSubstitute;
using SharpRepository.Repository;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Core.UnitTest.TestUtils;
using DnsClient;
using DnsClient.Protocol;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using SharpRepository.InMemoryRepository;
using Xunit;
using Xunit.Abstractions;
using Constants = Catalyst.Node.Common.Helpers.Config.Constants;
using Dns = Catalyst.Node.Common.Helpers.Network.Dns;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public class PeerDiscoveryUnitTest : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private readonly IDns _dns;
        private readonly IRepository<Peer> _peerRepository;
        private readonly ILogger _logger;
        private readonly ILookupClient _lookupClient;

        public PeerDiscoveryUnitTest(ITestOutputHelper output) : base(output)
        {
            _peerRepository = Substitute.For<IRepository<Peer>>();
            _logger = Substitute.For<ILogger>();
            _lookupClient = Substitute.For<ILookupClient>();
            _dns = new Dns(_lookupClient);

            _config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();   
        }

        [Fact]
        public void ResolvesIPeerDiscoveryCorrectly()
        {
            WriteLogsToFile = true;
            WriteLogsToTestOutput = true;

            ConfigureContainerBuilder(_config);

            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(_currentTestName))
            {
                var peerDiscovery = container.Resolve<IPeerDiscovery>();
                Assert.NotNull(peerDiscovery);
                peerDiscovery.Should().BeOfType(typeof(PeerDiscovery));
                Assert.NotNull(peerDiscovery.Dns);
                peerDiscovery.Dns.Should().BeOfType(typeof(Dns));
                Assert.NotNull(peerDiscovery.Logger);
                peerDiscovery.Logger.Should().BeOfType(typeof(Logger));
                Assert.NotNull(peerDiscovery.SeedNodes);
                peerDiscovery.SeedNodes.Should().BeOfType(typeof(List<string>));
                Assert.NotNull(peerDiscovery.Peers);
                peerDiscovery.Peers.Should().BeOfType(typeof(List<IPEndPoint>));
                Assert.NotNull(peerDiscovery.PeerRepository);
                peerDiscovery.PeerRepository.Should().BeOfType(typeof(InMemoryRepository<Peer>));
            }
        }

        [Fact]
        public void CanParseDnsNodesFromConfig()
        {
            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);
            
            peerDiscovery.ParseDnsServersFromConfig(_config);
            peerDiscovery.SeedNodes.Should().NotBeNullOrEmpty();
        }
        
        [Fact]
        public async Task CanGetSeedNodesFromDns()
        {           
            var urlList = new List<string>();
            var domain1 = "seed1.network.atlascity.io";
            var domain2 = "seed2.network.atlascity.io";
            urlList.Add(domain1);
            urlList.Add(domain2);
            
            CreateFakeLookupResult(domain1, "192.0.2.2:42069", domain1);
            CreateFakeLookupResult(domain2,"192.0.2.2:42069", domain2);
            
            var peerDiscovery = new PeerDiscovery(_dns, _peerRepository, _config, _logger);
            
            await peerDiscovery.GetSeedNodesFromDns(urlList);

            peerDiscovery.Peers.Should().NotBeNullOrEmpty();
            peerDiscovery.Peers.Should().HaveCount(2);
            peerDiscovery.Peers.Should().NotContainNulls();
            peerDiscovery.Peers.Should().ContainItemsAssignableTo<IPEndPoint>();
        }
        
        private void CreateFakeLookupResult(string domainName, string seed, string value)
        {
            var queryResponse = Substitute.For<IDnsQueryResponse>();
            var answers = new List<DnsResourceRecord>
            {
                new TxtRecord(new ResourceRecordInfo(domainName, ResourceRecordType.TXT, QueryClass.CS, 10, 32),
                    new[] {seed}, new[] {value}
                )
            };

            queryResponse.Answers.Returns(answers);
            _lookupClient.QueryAsync(Arg.Is(domainName), Arg.Any<QueryType>())
               .Returns(Task.FromResult(queryResponse));
        }
        
    }
}
