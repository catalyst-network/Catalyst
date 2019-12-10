using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Lib.Config;
using Catalyst.TestUtils;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests
{
    public class ConfigApiTest : FileSystemBasedTest
    {
        private IConfigApi ConfigApi;

        const string apiAddress = "/ip4/127.0.0.1/tcp/";
        const string gatewayAddress = "/ip4/127.0.0.1/tcp/";

        public ConfigApiTest(ITestOutputHelper output) : base(output)
        {
            ConfigApi = new ConfigApi(FileSystem);
        }
        
        [Fact]
        public async Task Get_Entire_Config()
        {
            var config = await ConfigApi.GetAsync();
            
            Assert.StartsWith(apiAddress, config["Addresses"]["API"].Value<string>());
        }

        [Fact]
        public async Task Get_Scalar_Key_Value()
        {
            var api = await ConfigApi.GetAsync("Addresses.API");
            Assert.StartsWith(apiAddress, api.Value<string>());
        }

        [Fact]
        public async Task Get_Object_Key_Value()
        {
            var addresses = await ConfigApi.GetAsync("Addresses");
            Assert.StartsWith(apiAddress, addresses["API"].Value<string>());
            Assert.StartsWith(gatewayAddress, addresses["Gateway"].Value<string>());
        }

        [Fact]
        public void Keys_are_Case_Sensitive()
        {
            var api = ConfigApi.GetAsync("Addresses.API").Result;
            Assert.StartsWith(apiAddress, api.Value<string>());

            ExceptionAssert.Throws<Exception>(() =>
            {
                var x = ConfigApi.GetAsync("Addresses.api").Result;
            });
        }

        [Fact]
        public async Task Set_String_Value()
        {
            const string key = "foo";
            const string value = "foobar";
            await ConfigApi.SetAsync(key, value);
            Assert.Equal(value, await ConfigApi.GetAsync(key));
        }

        [Fact]
        public async Task Set_JSON_Value()
        {
            const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
            JToken value = JToken.Parse("['http://example.io']");
            await ConfigApi.SetAsync(key, value);
            Assert.Equal("http://example.io", ConfigApi.GetAsync(key).Result[0]);
        }
    }
}
