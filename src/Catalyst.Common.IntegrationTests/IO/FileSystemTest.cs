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

namespace Catalyst.Common.IntegrationTests.IO
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public sealed class FileSystemTest 
    {
        private readonly CommonFileSystem _fileSystem;
        private readonly string _sourceFolder;

        public FileSystemTest()
        {
            _fileSystem = new CommonFileSystem();

            _sourceFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Constants.CatalystDataDir);

            Setup();
        }

        private void Setup()
        {          
            var targetConfigFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ConfigSubFolder);

            Console.WriteLine("DISPLAY Setup() targetConfigFolder :: " + targetConfigFolder);
            Console.WriteLine("DISPLAY Setup() _sourceFolder :: " + _sourceFolder);

            new ConfigCopier().RunConfigStartUp(targetConfigFolder, Catalyst.Common.Config.Network.Dev, _sourceFolder, overwrite: true);
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [InlineData("C:\\rubbishlocation\\fake\\technodisco")]
        [InlineData("gandolf\\treasure")]
        [InlineData("L:\\123\\fake")]
        public void Save_NonExistant_Data_Directory_Must_Fail(string path)
        {
            Console.WriteLine("DISPLAY path :: " + path);
            _fileSystem.SetCurrentPath(path).Should().BeFalse();
        }
               
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Existant_Data_Directory_Must_Succeed()
        {
            Console.WriteLine("DISPLAY _sourceFolder :: " + _sourceFolder);

            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Data_Directory_New_Instance_Must_Load_With_New_Data_Directory()
        {
            Console.WriteLine("DISPLAY _sourceFolder :: " + _sourceFolder);
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();

            var fileSystem = new CommonFileSystem();

            fileSystem.GetCatalystDataDir().FullName.ToLower().Should().Be(_fileSystem.GetCatalystDataDir().FullName.ToLower());
        }
    }
}
