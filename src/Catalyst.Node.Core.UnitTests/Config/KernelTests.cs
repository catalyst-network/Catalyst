using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Node.Core.Config;
using Catalyst.Node.Core.UnitTest.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigCopierTests : FileSystemBasedTest
    {
        private ConfigCopier _configCopier;

        public ConfigCopierTests(ITestOutputHelper output) : base(output)
        {
            _configCopier = new ConfigCopier();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Create_Folder_If_Needed()
        {
            var currentDirectory = _fileSystem.GetCatalystHomeDir();
            currentDirectory.Exists.Should().BeFalse("otherwise the test is not relevant");

            var network = NodeOptions.Networks.devnet;
            _configCopier.RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);

            currentDirectory.EnumerateFiles()
               .Select(f => f.Name).ToList().Should().BeEquivalentTo(expectedFileList);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_Network_File()
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(
                Constants.NetworkConfigFile(NodeOptions.Networks.mainnet));
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_Serilog_File()
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(Constants.SerilogJsonConfigFile);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_Components_File()
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(Constants.ComponentsJsonConfigFile);
        }


        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName)
        {
            var currentDirectory = _fileSystem.GetCatalystHomeDir();
            currentDirectory.Create(); currentDirectory.Refresh();
            var existingFileInfo = new FileInfo(fileName);
            existingFileInfo.Create(); existingFileInfo.Refresh();

            currentDirectory.Exists.Should().BeTrue("otherwise the test is not relevant");
            existingFileInfo.Exists.Should().BeTrue("otherwise the test is not relevant");

            var network = NodeOptions.Networks.testnet;
            _configCopier.RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);

            currentDirectory.EnumerateFiles()
               .Select(f => f.Name).ToList().Should().BeEquivalentTo(expectedFileList);

            existingFileInfo.Length.Should().Be(0,
                "the bogus file should not have been overwritten");
        }

        private static string[] GetExpectedFileList(NodeOptions.Networks network)
        {
            var expectedFileList = new[]
            {
                Constants.SerilogJsonConfigFile,
                Constants.ComponentsJsonConfigFile,
                Constants.NetworkConfigFile(network)
            };
            return expectedFileList;
        }
    }
}
