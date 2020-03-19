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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Core.Lib.Config;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using NUnit.Framework;


namespace Catalyst.Node.POA.CE.Tests.UnitTests.Config
{
    public sealed class ConfigCopierTests : FileSystemBasedTest
    {
        public ConfigCopierTests(TestContext output) : base(output) { }

        private sealed class ConfigFilesOverwriteTestData : List<object[]>
        {
            public ConfigFilesOverwriteTestData()
            {
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Mainnet), NetworkType.Mainnet });
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Testnet), NetworkType.Testnet });
                Add(new object[] { Constants.NetworkConfigFile(NetworkType.Devnet), NetworkType.Devnet });
                Add(new object[] { Constants.SerilogJsonConfigFile, NetworkType.Devnet });
            }
        }

        [Theory]
        [TestCase(typeof(ConfigFilesOverwriteTestData))]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Not_Overwrite_An_Existing_Config_File(string moduleFileName,
            NetworkType network)
        {
            RunConfigStartUp_Should_Not_Overwrite_Existing_Files(moduleFileName, network);
        }

        private void RunConfigStartUp_Should_Not_Overwrite_Existing_Files(string fileName,
            NetworkType network = NetworkType.Devnet)
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();
            currentDirectory.Create();
            currentDirectory.Refresh();
            var existingFileInfo = new FileInfo(Path.Combine(currentDirectory.FullName, fileName));
            if (existingFileInfo.Directory != null && !existingFileInfo.Directory.Exists)
                existingFileInfo.Directory.Create();

            existingFileInfo.Create();
            existingFileInfo.Refresh();

            currentDirectory.Exists.Should().BeTrue("otherwise the test is not relevant");
            existingFileInfo.Exists.Should().BeTrue("otherwise the test is not relevant");

            new TestConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network).ToList();
            var configFiles = EnumerateConfigFiles(currentDirectory);

            configFiles.Should().BeEquivalentTo(expectedFileList);

            existingFileInfo.Length.Should().Be(0,
                "the bogus file should not have been overwritten");
        }

        private static IEnumerable<string> EnumerateConfigFiles(DirectoryInfo currentDirectory)
        {
            var filesOnDisk = currentDirectory.EnumerateFiles()
               .Where(f => f.Extension.Equals(".json", StringComparison.CurrentCultureIgnoreCase))
               .Select(f => f.Name);
            return filesOnDisk;
        }

        private static IEnumerable<string> GetExpectedFileList(NetworkType network)
        {
            var requiredConfigFiles = new[]
            {
                Constants.NetworkConfigFile(network),
                Constants.SerilogJsonConfigFile
            };
            return requiredConfigFiles;
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void RunConfigStartUp_Should_Create_Folder_If_Needed()
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();
            currentDirectory.Exists.Should().BeFalse("otherwise the test is not relevant");

            var network = NetworkType.Devnet;
            new TestConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

            var expectedFileList = GetExpectedFileList(network);
            var configFiles = EnumerateConfigFiles(currentDirectory);
            configFiles.Should().BeEquivalentTo(expectedFileList);
        }

        [Test]
        public async Task Can_Copy_Overridden_Network_File()
        {
            var overrideFile = "TestOverride.json";
            var currentDirectory = FileSystem.GetCatalystDataDir();
            await FileSystem.WriteTextFileToCddAsync(overrideFile,
                File.ReadAllText(Path.Combine(Constants.ConfigSubFolder,
                    Constants.NetworkConfigFile(NetworkType.Devnet)))).ConfigureAwait(false);
            new TestConfigCopier().RunConfigStartUp(currentDirectory.FullName, NetworkType.Devnet, null, true,
                overrideFile);
            File.Exists(Path.Combine(currentDirectory.FullName, overrideFile)).Should().BeTrue();
        }

        internal class TestConfigCopier : ConfigCopier
        {
            protected override IEnumerable<string> RequiredConfigFiles(NetworkType network,
                string overrideNetworkFile = null)
            {
                return new List<string>(GetExpectedFileList(network));
            }
        }
    }
}
