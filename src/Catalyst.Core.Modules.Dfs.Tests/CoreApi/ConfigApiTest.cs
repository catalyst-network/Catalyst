using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class ConfigApiTest
    {
        const string apiAddress = "/ip4/127.0.0.1/tcp/";
        const string gatewayAddress = "/ip4/127.0.0.1/tcp/";

        [Fact]
        public async Task Get_Entire_Config()
        {
            var ipfs = TestFixture.Ipfs;
            var config = await ipfs.Config.GetAsync();
            Assert.StartsWith(config["Addresses"]["API"].Value<string>(), apiAddress);
        }

        [Fact]
        public async Task Get_Scalar_Key_Value()
        {
            var ipfs = TestFixture.Ipfs;
            var api = await ipfs.Config.GetAsync("Addresses.API");
            Assert.StartsWith(api.Value<string>(), apiAddress);
        }

        [Fact]
        public async Task Get_Object_Key_Value()
        {
            var ipfs = TestFixture.Ipfs;
            var addresses = await ipfs.Config.GetAsync("Addresses");
            Assert.StartsWith(addresses["API"].Value<string>(), apiAddress);
            Assert.StartsWith(addresses["Gateway"].Value<string>(), gatewayAddress);
        }

        [Fact]
        public void Keys_are_Case_Sensitive()
        {
            var ipfs = TestFixture.Ipfs;
            var api = ipfs.Config.GetAsync("Addresses.API").Result;
            Assert.StartsWith(api.Value<string>(), apiAddress);

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
            var ipfs = TestFixture.Ipfs;
            await ipfs.Config.SetAsync(key, value);
            Assert.Equal(value, await ipfs.Config.GetAsync(key));
        }

        [Fact]
        public async Task Set_JSON_Value()
        {
            const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
            JToken value = JToken.Parse("['http://example.io']");
            var ipfs = TestFixture.Ipfs;
            await ipfs.Config.SetAsync(key, value);
            Assert.Equal("http://example.io", ipfs.Config.GetAsync(key).Result[0]);
        }
    }
}
