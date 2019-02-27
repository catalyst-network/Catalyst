using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Core.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Node.Common.UnitTests.TestUtils;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigCopierTests : FileSystemBasedTest
    {
        private ConfigCopier _configCopier;
        public static List<object[]> ConfigFiles;
        static ConfigCopierTests()
        {
            ConfigFiles = Constants.AllModuleFiles.Select(m => new object[] { m, NodeOptions.Networks.testnet }).ToList();
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(NodeOptions.Networks.mainnet), NodeOptions.Networks.mainnet });
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(NodeOptions.Networks.testnet), NodeOptions.Networks.testnet });
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(NodeOptions.Networks.devnet), NodeOptions.Networks.devnet });
            ConfigFiles.Add(new object[] { Constants.SerilogJsonConfigFile, NodeOptions.Networks.devnet });
            ConfigFiles.Add(new object[] { Constants.ComponentsJsonConfigFile, NodeOptions.Networks.devnet });
        }


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

            var modulesDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, Constants.ModulesSubFolder));

            var network = NodeOptions.Networks.devnet;
            _configCopier.RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);
            var configFiles = EnumerateConfigFiles(currentDirectory, modulesDirectory);
            configFiles.Should().BeEquivalentTo(expectedFileList);
        }

        [Theory]
        [MemberData(nameof(ConfigFiles))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_An_Existing_Config_File(string moduleFileName, NodeOptions.Networks network)
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(moduleFileName, network);
        }

        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName, NodeOptions.Networks network = NodeOptions.Networks.testnet)
        {
            var currentDirectory = _fileSystem.GetCatalystHomeDir();
            currentDirectory.Create(); currentDirectory.Refresh();
            var existingFileInfo = new FileInfo(Path.Combine(currentDirectory.FullName, fileName));
            if(!existingFileInfo.Directory.Exists) existingFileInfo.Directory.Create();
            existingFileInfo.Create(); existingFileInfo.Refresh();

            var modulesDirectory = new DirectoryInfo(Path.Combine(currentDirectory.FullName, Constants.ModulesSubFolder));

            currentDirectory.Exists.Should().BeTrue("otherwise the test is not relevant");
            existingFileInfo.Exists.Should().BeTrue("otherwise the test is not relevant");

            _configCopier.RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network).ToList();
            var configFiles = EnumerateConfigFiles(currentDirectory, modulesDirectory);

            configFiles.Should().BeEquivalentTo(expectedFileList);

            existingFileInfo.Length.Should().Be(0,
                "the bogus file should not have been overwritten");
        }

        private static IEnumerable<string> EnumerateConfigFiles(DirectoryInfo currentDirectory,
            DirectoryInfo modulesDirectory)
        {
            var filesOnDisk = currentDirectory.EnumerateFiles()
               .Select(f => f.Name)
               .Concat(modulesDirectory.EnumerateFiles()
                   .Select(f => Path.Combine(Constants.ModulesSubFolder, f.Name)));
            return filesOnDisk;
        }

        private IEnumerable<string> GetExpectedFileList(NodeOptions.Networks network)
        {
            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile
            }.Concat(Constants.AllModuleFiles);
            return requiredConfigFiles;
        }
    }
}
