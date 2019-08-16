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
using Catalyst.Common.Types;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.UnitTests.Config
{
    public sealed class ConfigCopierTests : FileSystemBasedTest
    {
        public ConfigCopierTests(ITestOutputHelper output) : base(output) { }

        private sealed class ConfigFilesOverwriteTestData : TheoryData<string, NetworkTypes>
        {
            public ConfigFilesOverwriteTestData()
            {
                Add(Constants.NetworkConfigFile(NetworkTypes.Main), NetworkTypes.Main);
                Add(Constants.NetworkConfigFile(NetworkTypes.Test), NetworkTypes.Test);
                Add(Constants.NetworkConfigFile(NetworkTypes.Dev), NetworkTypes.Dev);
                Add(Constants.SerilogJsonConfigFile, NetworkTypes.Dev);
                Add(Constants.ComponentsJsonConfigFile, NetworkTypes.Dev);
                Add(Constants.MessageHandlersConfigFile, NetworkTypes.Dev);
            }
        }

        [Theory]
        [ClassData(typeof(ConfigFilesOverwriteTestData))]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_An_Existing_Config_File(string moduleFileName,
            NetworkTypes networkTypes)
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(moduleFileName, networkTypes);
        }

        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName, NetworkTypes networkTypes)
        {
            networkTypes = networkTypes ?? NetworkTypes.Dev;
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

            new ConfigCopier().RunConfigStartUp(currentDirectory.FullName, networkTypes);

            var expectedFileList = GetExpectedFileList(networkTypes).ToList();
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

        private IEnumerable<string> GetExpectedFileList(NetworkTypes networkTypes)
        {
            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(networkTypes),
                Constants.ComponentsJsonConfigFile,
                Constants.SerilogJsonConfigFile,
                Constants.MessageHandlersConfigFile,
                Constants.RpcAuthenticationCredentialsFile
            };
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

            var network = NetworkTypes.Dev;
            new ConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);
            var configFiles = EnumerateConfigFiles(currentDirectory, modulesDirectory);
            configFiles.Should().BeEquivalentTo(expectedFileList);
        }
    }
}
