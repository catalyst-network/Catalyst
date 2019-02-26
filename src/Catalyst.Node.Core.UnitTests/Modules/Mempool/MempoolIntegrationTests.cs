using System.IO;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Config;
using Microsoft.Extensions.Configuration;
using NSec.Cryptography;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public class MempoolIntegrationTests : FileSystemBasedTest
    {
        public MempoolIntegrationTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .Build();
        }       
    }
}