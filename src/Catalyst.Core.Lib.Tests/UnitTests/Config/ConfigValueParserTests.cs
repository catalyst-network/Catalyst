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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Abstractions.Network;
using Catalyst.Core.Lib.Config;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Microsoft.Extensions.Configuration;
using Xunit.Sdk;

namespace Catalyst.Core.Lib.Tests.UnitTests.Config
{
    public sealed class ConfigValueParserTests
    {
        public ConfigValueParserTests()
        {
            //_configurationSection = Substitute.For<IConfigurationSection>();
            _ipEndpoint1 = IPEndPoint.Parse("127.0.0.1:5052");
            _ipEndpoint2 = IPEndPoint.Parse("127.0.0.1:5053");
            _sectionName = "DnsServers";
            var peerConfig = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("CatalystNodeConfiguration:Peer:" + _sectionName + ":0", _ipEndpoint1.ToString()),
                new KeyValuePair<string, string>("CatalystNodeConfiguration:Peer:" + _sectionName + ":1", _ipEndpoint2.ToString())
            };

            _configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(peerConfig).Build();
        }

        private readonly IConfigurationRoot _configurationRoot;
        private readonly IConfigurationSection _configurationSection;
        private readonly string _sectionName;
        private readonly IPEndPoint _ipEndpoint1;
        private readonly IPEndPoint _ipEndpoint2;

        [Fact]
        public void ConfigValueParser_Can_Parse_Multiple_IpEndpoints()
        {
            var endPoints = ConfigValueParser.GetIpEndpointArrValues(_configurationRoot, _sectionName);
            endPoints.Should().HaveCount(2);
            endPoints.Should().Contain(_ipEndpoint1);
            endPoints.Should().Contain(_ipEndpoint2);
        }

        [Fact]
        public void ConfigValueParser_Can_Parse_Multiple_IpEndpoints_As_Strings()
        {
            var endPoints = ConfigValueParser.GetStringArrValues(_configurationRoot, _sectionName);
            endPoints.Should().HaveCount(2);
            endPoints.Should().Contain(_ipEndpoint1.ToString());
            endPoints.Should().Contain(_ipEndpoint2.ToString());
        }
    }

}
