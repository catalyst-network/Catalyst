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
using System.IO;
using FluentAssertions;
using Xunit;
using Catalyst.TestUtils;
using Catalyst.Common.Config;
using CommonFileSystem = Catalyst.Common.FileSystem.FileSystem;
using Xunit.Abstractions;

namespace Catalyst.Node.IntegrationTests.IO
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public sealed class FileSystemTest : FileSystemBasedTest
    {
        private readonly CommonFileSystem _fileSystem;
        private readonly string _sourceFolder;

        public FileSystemTest(ITestOutputHelper output) : base(output)
        {
            _fileSystem = new CommonFileSystem();

            _sourceFolder = Setup();
        }

        private string Setup()
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();

            currentDirectory.Exists.Should().BeFalse("otherwise the test is not relevant");

            var modulesDirectory =
                new DirectoryInfo(Path.Combine(currentDirectory.FullName, Constants.ModulesSubFolder));

            var network = Catalyst.Common.Config.Network.Dev;
            new ConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);
            return currentDirectory.FullName;
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("C:\\rubbishlocation\\fake\\technodisco")]
        [InlineData("gandolf\\treasure")]
        [InlineData("L:\\123\\fake")]
        public void Save_NonExistant_Data_Directory_Must_Fail(string path)
        {
            _fileSystem.SetCurrentPath(path).Should().BeFalse();
        }
               
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Existant_Data_Directory_Must_Succeed()
        {
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Data_Directory_New_Instance_Must_Load_With_New_Data_Directory()
        {
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();

            var fileSystem = new CommonFileSystem();

            fileSystem.GetCatalystDataDir().FullName.ToLower().Should().Be(_fileSystem.GetCatalystDataDir().FullName.ToLower());
        }

        [Fact (Skip="Temporary Skip")]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Data_Directory_Several_Times_New_Instance_Must_Load_With_New_Data_Directory()
        {
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();

            var fileSystem = new CommonFileSystem();
            fileSystem.GetCatalystDataDir().FullName.ToLower().Should().Be(_fileSystem.GetCatalystDataDir().FullName.ToLower());

            GenerateFileSystem();

            var changeDataDir = Setup(); 
            fileSystem.SetCurrentPath(changeDataDir).Should().BeTrue();

            var fileSystemRetriever = new CommonFileSystem();

            fileSystem.GetCatalystDataDir().FullName.ToLower().Should()
                .Be(fileSystemRetriever.GetCatalystDataDir().FullName.ToLower());
        }
    }
}
