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
using Catalyst.Cli.Options;
using FluentAssertions;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public sealed class CatalystCliOptionsTest
    {
        private ChangeDataFolderOptions _changeDataFolderOptions;

        public static IEnumerable<object[]> WorkingPaths =>
            new List<object[]>
            {
                new object[] {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "forward", "Path", "Allocation")},
                new object[] {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Data", "Concept")},
                new object[] {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Azure", "CloudTechnology")},
                new object[] {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "PencilCamera", "FolderFriend")}
            };

        public CatalystCliOptionsTest()
        {
            _changeDataFolderOptions = new ChangeDataFolderOptions();
        }

        [Theory]
        [MemberData(nameof(WorkingPaths))]
        public void ChangeDataFolder_Set_Data_Folder_Property_Valid_Path_Must_Store_Successfully(string path)
        {
            _changeDataFolderOptions.DataFolder = path;

            _changeDataFolderOptions.DataFolder.Should().Be(path);
        }

        [Theory]
        [InlineData("'q*Pen\0'cilL:\\123\\fak / e'")]
        [InlineData("'phmtbt*\0 3 / lopg'")]
        [InlineData("'\0/gthgt5\000*\0 3 / woigwogmom4t4040gkwvkinwewowegmvowpmgopweWe will WIN anyway, but it would be much easier if the g5 undergmowgewgwgwegwegegegeg'")]
        public void ChangeDataFolder_Does_Not_Set_Data_Folder_Property_Invalid_Path(string path)
        {
            _changeDataFolderOptions.DataFolder = path;

            _changeDataFolderOptions.DataFolder.Should().BeNullOrEmpty();
        }
    }
}
