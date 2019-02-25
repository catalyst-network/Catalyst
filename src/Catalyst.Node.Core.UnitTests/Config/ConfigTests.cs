using FluentAssertions;
using SharpRepository.Repository;
using System;
using System.IO;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigTests
    {
        [Theory]
        [InlineData("devnet")]
        [InlineData("testnet")]
        [InlineData("mainnet")]
        public void Config_Should_Contain_a_valid_storage_module(string network)
        {
            var dataDir = Path.Combine(Environment.CurrentDirectory, "Config");
            var optionBuilder = new NodeOptionsBuilder("debug", dataDir, network);
            var networkConfiguration = optionBuilder.LoadNetworkConfig(network, dataDir);
            var configurationSection = networkConfiguration.GetSection("PersistenceConfiguration");
            var persistenceConfiguration = RepositoryFactory.BuildSharpRepositoryConfiguation(configurationSection);

            persistenceConfiguration.HasRepository.Should().BeTrue();
            persistenceConfiguration.DefaultRepository.Should().NotBeNullOrEmpty();
            persistenceConfiguration.DefaultRepository.Should().Be("inMemoryNoCaching");
        }
    }
}
