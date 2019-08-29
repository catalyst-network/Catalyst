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

using System.IO;
using Catalyst.Core.Extensions;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Extensions
{
    public sealed class DirectoryInfoExtensionsTests : FileSystemBasedTest
    {
        private readonly string _subDirectory = "subdir";

        public DirectoryInfoExtensionsTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SubDirectoryInfo_Returns_Directory_Info_If_SubDirectory_Exists()
        {
            var dirInfo = FileSystem.GetCatalystDataDir();
            Directory.CreateDirectory(Path.Combine(dirInfo.FullName, _subDirectory));

            var subDirInfo = FileSystem.GetCatalystDataDir().SubDirectoryInfo(_subDirectory);

            subDirInfo.Should().NotBe(null);
            subDirInfo.Exists.Should().BeTrue();
        }

        [Fact]
        public void SubDirectoryInfo_Returns_Directory_Info_If_SubDirectory_Doesnt_Exist()
        {
            var subDirInfo = FileSystem.GetCatalystDataDir().SubDirectoryInfo(_subDirectory);

            subDirInfo.Should().NotBe(null);
            subDirInfo.Exists.Should().BeFalse();
        }

        [Fact]
        public void SubDirectoryInfo_Has_Correct_Parent_Directory()
        {
            var parentDirInfo = FileSystem.GetCatalystDataDir();
            var subDirInfo = FileSystem.GetCatalystDataDir().SubDirectoryInfo(_subDirectory);

            subDirInfo.Parent.FullName.Should().BeEquivalentTo(parentDirInfo.FullName);
        }
    }
}
