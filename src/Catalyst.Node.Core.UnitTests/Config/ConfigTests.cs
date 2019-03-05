using FluentAssertions;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Catalyst.Node.Common.Config;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.P2P;
using Microsoft.Extensions.Configuration;
using Networker.Common;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigTests
    {
        public static readonly List<object[]> Networks;

        static ConfigTests()
        {
            Networks = Enumeration.GetAll<Network>().Select(n => new[] {n as object}).ToList();
        }

        [Theory]
        [MemberData(nameof(Networks))]
        public void Config_Should_Contain_a_valid_storage_module(Network network)
        {
            var configFile = Path.Combine(Environment.CurrentDirectory, Constants.ConfigSubFolder, Constants.NetworkConfigFile(network));
            var networkConfiguration = new ConfigurationBuilder().AddJsonFile(configFile).Build();
            var configurationSection = networkConfiguration
               .GetSection("CatalystNodeConfiguration")
               .GetSection("PersistenceConfiguration");
            var persistenceConfiguration = RepositoryFactory.BuildSharpRepositoryConfiguation(configurationSection);

            persistenceConfiguration.HasRepository.Should().BeTrue();
            persistenceConfiguration.DefaultRepository.Should().NotBeNullOrEmpty();
            persistenceConfiguration.DefaultRepository.Should().Be("inMemoryNoCaching");
        }
    }
}
