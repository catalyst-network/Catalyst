using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class ConfigApiTest
    {
        private IDfs ipfs;

        const string apiAddress = "/ip4/127.0.0.1/tcp/";
        const string gatewayAddress = "/ip4/127.0.0.1/tcp/";

        public ConfigApiTest(ITestOutputHelper output)
        {
            ipfs = new TestFixture(output).Ipfs;      
        }
        
        [Fact]
        public async Task Get_Entire_Config()
        {
            var config = await ipfs.Config.GetAsync();
            
            Assert.StartsWith(apiAddress, config["Addresses"]["API"].Value<string>());
        }

        [Fact]
        public async Task Get_Scalar_Key_Value()
        {
            var api = await ipfs.Config.GetAsync("Addresses.API");
            Assert.StartsWith(apiAddress, api.Value<string>());
        }

        [Fact]
        public async Task Get_Object_Key_Value()
        {
            var addresses = await ipfs.Config.GetAsync("Addresses");
            Assert.StartsWith(apiAddress, addresses["API"].Value<string>());
            Assert.StartsWith(gatewayAddress, addresses["Gateway"].Value<string>());
        }

        [Fact]
        public void Keys_are_Case_Sensitive()
        {
            var api = ipfs.Config.GetAsync("Addresses.API").Result;
            Assert.StartsWith(apiAddress, api.Value<string>());

            ExceptionAssert.Throws<Exception>(() =>
            {
                var x = ipfs.Config.GetAsync("Addresses.api").Result;
            });
        }

        [Fact]
        public async Task Set_String_Value()
        {
            const string key = "foo";
            const string value = "foobar";
            await ipfs.Config.SetAsync(key, value);
            Assert.Equal(value, await ipfs.Config.GetAsync(key));
        }

        [Fact]
        public async Task Set_JSON_Value()
        {
            const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
            JToken value = JToken.Parse("['http://example.io']");
            await ipfs.Config.SetAsync(key, value);
            Assert.Equal("http://example.io", ipfs.Config.GetAsync(key).Result[0]);
        }
    }
}
