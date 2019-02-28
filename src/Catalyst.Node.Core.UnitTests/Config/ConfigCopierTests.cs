using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.Config;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Node.Common.UnitTests.TestUtils;

namespace Catalyst.Node.Core.UnitTest.Config
{
    public class ConfigCopierTests : FileSystemBasedTest
    {
        private readonly ConfigCopier _configCopier;
        public static readonly List<object[]> ConfigFiles;
        static ConfigCopierTests()
        {
            ConfigFiles = Constants.AllModuleFiles.Select(m => new object[] { m, Network.Test }).ToList();
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(Network.Main), Network.Main });
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(Network.Test), Network.Test });
            ConfigFiles.Add(new object[] { Constants.NetworkConfigFile(Network.Dev), Network.Dev });
            ConfigFiles.Add(new object[] { Constants.SerilogJsonConfigFile, Network.Dev });
            ConfigFiles.Add(new object[] { Constants.ComponentsJsonConfigFile, Network.Dev });
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

            var network = Network.Dev;
            _configCopier.RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);
            var configFiles = EnumerateConfigFiles(currentDirectory, modulesDirectory);
            configFiles.Should().BeEquivalentTo(expectedFileList);
        }

        [Theory]
        [MemberData(nameof(ConfigFiles))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_An_Existing_Config_File(string moduleFileName, Network network)
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(moduleFileName, network);
        }

        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName, Network network)
        {
            network = network ?? Network.Dev;
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

        private IEnumerable<string> GetExpectedFileList(Network network)
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
