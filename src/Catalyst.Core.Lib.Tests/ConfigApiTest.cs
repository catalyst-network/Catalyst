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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Config;
using Catalyst.TestUtils;
using Makaretu.Dns;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests
{
    public sealed class ConfigApiTest : FileSystemBasedTest
    {
        private readonly IConfigApi _configApi;

        private const string ApiAddress = "/ip4/127.0.0.1/tcp/";
        private const string GatewayAddress = "/ip4/127.0.0.1/tcp/";

        public ConfigApiTest(ITestOutputHelper output) : base(output)
        {
            var dfsOptions = new DfsOptions(Substitute.For<BlockOptions>(), Substitute.For<DiscoveryOptions>(), new RepositoryOptions(FileSystem, Constants.DfsDataSubDir), Substitute.For<KeyChainOptions>(), Substitute.For<SwarmOptions>(), Substitute.For<IDnsClient>());
            _configApi = new ConfigApi(dfsOptions);
        }
        
        [Fact]
        public async Task Get_Entire_Config()
        {
            var config = await _configApi.GetAsync();
            
            Assert.StartsWith(ApiAddress, config["Addresses"]["API"].Value<string>());
        }

        [Fact]
        public async Task Get_Scalar_Key_Value()
        {
            var api = await _configApi.GetAsync("Addresses.API");
            Assert.StartsWith(ApiAddress, api.Value<string>());
        }

        [Fact]
        public async Task Get_Object_Key_Value()
        {
            var addresses = await _configApi.GetAsync("Addresses");
            Assert.StartsWith(ApiAddress, addresses["API"].Value<string>());
            Assert.StartsWith(GatewayAddress, addresses["Gateway"].Value<string>());
        }

        [Fact]
        public void Keys_are_Case_Sensitive()
        {
            var api = _configApi.GetAsync("Addresses.API").Result;
            Assert.StartsWith(ApiAddress, api.Value<string>());

            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _configApi.GetAsync("Addresses.api").Result;
            });
        }

        [Fact]
        public async Task Set_String_Value()
        {
            const string key = "foo";
            const string value = "foobar";
            await _configApi.SetAsync(key, value);
            Assert.Equal(value, await _configApi.GetAsync(key));
        }

        [Fact]
        public async Task Set_JSON_Value()
        {
            const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
            var value = JToken.Parse("['http://example.io']");
            await _configApi.SetAsync(key, value);
            Assert.Equal("http://example.io", _configApi.GetAsync(key).Result[0]);
        }
    }
}
