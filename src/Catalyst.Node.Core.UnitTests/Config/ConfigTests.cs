#region LICENSE
/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
* 
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Enumerator;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigTests
    {
        public static readonly List<object[]> Networks;

        static ConfigTests() { Networks = Enumeration.GetAll<Network>().Select(n => new[] {n as object}).ToList(); }

        [Theory]
        [MemberData(nameof(Networks))]
        public void Config_Should_Contain_a_valid_storage_module(Network network)
        {
            var configFile = Path.Combine(Environment.CurrentDirectory, Constants.ConfigSubFolder,
                Constants.NetworkConfigFile(network));
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