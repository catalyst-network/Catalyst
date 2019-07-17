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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.UnitTests.Config
{
    public sealed class ConfigCopierTests : FileSystemBasedTest
    {
        public ConfigCopierTests(ITestOutputHelper output) : base(output) { }

        private sealed class ConfigFilesOverwriteTestData : TheoryData<string, Network>
        {
            public ConfigFilesOverwriteTestData()
            {
                Add(Constants.NetworkConfigFile(Network.Main), Network.Main);
                Add(Constants.NetworkConfigFile(Network.Test), Network.Test);
                Add(Constants.NetworkConfigFile(Network.Dev), Network.Dev);
                Add(Constants.SerilogJsonConfigFile, Network.Dev);
                Add(Constants.ComponentsJsonConfigFile, Network.Dev);
                Add(Constants.MessageHandlersConfigFile, Network.Dev);
            }
        }

        [Theory]
        [ClassData(typeof(ConfigFilesOverwriteTestData))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_An_Existing_Config_File(string moduleFileName,
            Network network)
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(moduleFileName, network);
        }

        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName, Network network)
        {
            network = network ?? Network.Dev;
            var currentDirectory = FileSystem.GetCatalystDataDir();
            currentDirectory.Create();
            currentDirectory.Refresh();
            var existingFileInfo = new FileInfo(Path.Combine(currentDirectory.FullName, fileName));
            if (existingFileInfo.Directory != null && !existingFileInfo.Directory.Exists)
            {
                existingFileInfo.Directory.Create();
            }

            existingFileInfo.Create();
            existingFileInfo.Refresh();

            var modulesDirectory =
                new DirectoryInfo(Path.Combine(currentDirectory.FullName, Constants.ModulesSubFolder));

            currentDirectory.Exists.Should().BeTrue("otherwise the test is not relevant");
            existingFileInfo.Exists.Should().BeTrue("otherwise the test is not relevant");

            new ConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

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
                Constants.SerilogJsonConfigFile,
                Constants.MessageHandlersConfigFile
            }.Concat(Constants.AllModuleFiles);
            return requiredConfigFiles;
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Create_Folder_If_Needed()
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();
            currentDirectory.Exists.Should().BeFalse("otherwise the test is not relevant");

            var modulesDirectory =
                new DirectoryInfo(Path.Combine(currentDirectory.FullName, Constants.ModulesSubFolder));

            var network = Network.Dev;
            new ConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);
            var configFiles = EnumerateConfigFiles(currentDirectory, modulesDirectory);
            configFiles.Should().BeEquivalentTo(expectedFileList);
        }
    }
}
