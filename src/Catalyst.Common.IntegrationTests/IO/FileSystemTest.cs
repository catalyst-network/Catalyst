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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Catalyst.Common.Interfaces.FileSystem;
using Dawn;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Catalyst.TestUtils;
using Serilog;
using System.Collections.Generic;


using CommonFileSystem = Catalyst.Common.FileSystem.FileSystem;

namespace Catalyst.Common.IntegrationTests.IO
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Trait(Traits.TestType, Traits.IntegrationTest)]
    public sealed class FileSystemTest : IDisposable
    {
        private CommonFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly string _configFilePointerName = "ConfigFilePointerTester.txt";
        private string _configFilepointerTestPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\" + _configFilePointerName;
       
        public static IEnumerable<object[]> DirectoryNames;

        private string _testDataDir = "\\.TestDir";
        static FileSystemTest()
        {
            DirectoryNames = new List<object[]>
            {
                new object[]
                {
                    "\\.TestConFigDir",
                },
                new object[]
                {
                    "\\.MyLocalDir",
                },
                new object[]
                {
                    "\\.CoreAppFolder",
                },
                new object[]
                {
                    "\\.TemporaryTestFolder",
                },
            };
        }

        public FileSystemTest()
        {
            _logger = Substitute.For<ILogger>();
        }

        public void Dispose()
        {
            DeleteTestDirectories();

            DeleteTestConfigFile();

            DeleteDirectory(_testDataDir);
        }

        public void DeleteTestConfigFile()
        {
            try
            {
                System.IO.File.Delete(_configFilepointerTestPath);
            }
            catch (Exception ex)
            {
                //we are not concerned
            }
        }

        private void DeleteTestDirectories()
        {
            try
            {
                DirectoryNames.SelectMany(m => m).ToList().Select(j => j as string)
                    .ToList().ForEach(m => DeleteDirectory(m));
            }
            catch (Exception ex)
            {
                //We are not too concerned about this
            }
        }

        private void DeleteDirectory(string path)
        {
            try
            {
                var dir = CommonFileSystem.FilePointerBaseLocation + path;

                Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                //We are not too concerned about this
            }
        }

        [Theory]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        [MemberData(nameof(DirectoryNames))]
        public void ConfigFilePointer_Should_Be_Created_As_It_Does_Not_Exist(string name)
        {
            _fileSystem = new CommonFileSystem(true, _logger, _configFilepointerTestPath, name);

            new DirectoryInfo(_fileSystem.DataDir).Exists.Should().BeTrue();
            _fileSystem.DataDir.Contains(name).Should().BeTrue();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void ConfigFilePointer_There_Should_Only_Be_One_ConfigFilePointer_Irrespective_Of_No_Runs()
        {
            DeleteDirectory(_testDataDir);

            var pointerDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            _fileSystem = new CommonFileSystem(true, _logger, _configFilepointerTestPath, _testDataDir);

            for(int i = 0; i < 10; i++)
            {
               new CommonFileSystem(true, _logger, _configFilepointerTestPath, _testDataDir);
            }

            Directory.GetFiles(pointerDir, _configFilePointerName).Should().HaveCount(1);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Get_Data_Directory_Should_Successfully_Read_ConfigPointerFile()
        {
            DeleteDirectory(_testDataDir);

            var fileSystem = new CommonFileSystem(true, _logger, _configFilepointerTestPath, _testDataDir);
            new DirectoryInfo(fileSystem.DataDir).Exists.Should().BeTrue();

            var fileSystemPreviousCheck = new CommonFileSystem(true, _logger, _configFilepointerTestPath, _testDataDir);
            fileSystemPreviousCheck.GetCatalystDataDir().FullName.Should().Be(fileSystem.GetCatalystDataDir().FullName);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Get_Data_Directory_Should_Handle_NonExistant_File_Location()
        {
            var fileSystem = new CommonFileSystem(true, _logger, _configFilepointerTestPath, "junkjunkie");

            var fileSystemTest = new CommonFileSystem();
            fileSystemTest.GetCatalystDataDir().FullName.Should().Be(fileSystem.GetCatalystDataDir().FullName);
        }
    }
}
