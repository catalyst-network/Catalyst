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

using System.Globalization;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Protocol.Network;
using Catalyst.TestUtils;
using FluentAssertions;
using NUnit.Framework;


namespace Catalyst.Node.POA.CE.Tests.IntegrationTests.IO
{
    /// <inheritdoc />
    /// <summary>
    ///     A base test class that can be used to offer inheriting tests a folder on which
    ///     to create files, logs, etc.
    /// </summary>
    [Property(Traits.TestType, Traits.IntegrationTest)]
    public sealed class FileSystemTest : FileSystemBasedTest
    {
        private readonly FileSystem _fileSystem;
        private readonly string _sourceFolder;

        public FileSystemTest() : base(TestContext.CurrentContext)
        {
            _fileSystem = new FileSystem();

            _sourceFolder = Setup();
        }

        private string Setup()
        {
            var currentDirectory = FileSystem.GetCatalystDataDir();

            var network = NetworkType.Devnet;
            new PoaConfigCopier().RunConfigStartUp(currentDirectory.FullName, network);

            return currentDirectory.FullName;
        }

        private bool CheckSavedPath(string path)
        {
            return _fileSystem.GetCatalystDataDir().FullName.ToLower(CultureInfo.InvariantCulture)
               .Equals(path.ToLower(CultureInfo.InvariantCulture));
        }

        [Theory]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        [TestCase("'\0'")]
        [TestCase("'xxx://gan\0'dolf\\treasu\re*&+'")]
        [TestCase("'q*Pen\0'cilL:\\123\\fak/e'")]
        public void Save_Invalid_Data_Directory_Must_Fail(string path)
        {
            _fileSystem.SetCurrentPath(path).Should().BeFalse();

            CheckSavedPath(path).Should().BeFalse();
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Existent_Data_Directory_Must_Succeed()
        {
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();

            CheckSavedPath(_sourceFolder).Should().BeTrue();
        }

        [Ignore("TODO: Data directory logic has been removed")]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Save_Data_Directory_Several_Times_New_Instance_Must_Load_With_New_Data_Directory()
        {
            _fileSystem.SetCurrentPath(_sourceFolder).Should().BeTrue();

            var fileSystem = new FileSystem();

            CheckSavedPath(_sourceFolder).Should().BeTrue();

            CreateUniqueTestDirectory();

            var changeDataDir = Setup();

            fileSystem.SetCurrentPath(changeDataDir).Should().BeTrue();

            CheckSavedPath(changeDataDir).Should().BeTrue();
        }
    }
}
