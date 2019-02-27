using FluentAssertions;
using SharpRepository.Repository;
using System;
using System.IO;
using Catalyst.Node.Common.P2P;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigTests
    {
        //TODO : Tomorrow
        //[Theory]
        //[InlineData(Network.Dev)]
        //[InlineData(Network.Main)]
        //[InlineData(Network.Test)]
        //public void Config_Should_Contain_a_valid_storage_module(Network network)
        //{
        //    var dataDir = Path.Combine(Environment.CurrentDirectory, "Config");
        //    var networkConfiguration = optionBuilder.LoadNetworkConfig(network, dataDir);
        //    var configurationSection = networkConfiguration.GetSection("PersistenceConfiguration");
        //    var persistenceConfiguration = RepositoryFactory.BuildSharpRepositoryConfiguation(configurationSection);

        //    persistenceConfiguration.HasRepository.Should().BeTrue();
        //    persistenceConfiguration.DefaultRepository.Should().NotBeNullOrEmpty();
        //    persistenceConfiguration.DefaultRepository.Should().Be("inMemoryNoCaching");
        //}
    }
}
