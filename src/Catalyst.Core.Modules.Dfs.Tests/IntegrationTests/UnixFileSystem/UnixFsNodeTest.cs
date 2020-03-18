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

using Catalyst.Core.Modules.Dfs.UnixFs;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.UnixFileSystem
{
    public class UnixFsNodeTest
    {
        [Fact]
        public void ToLink()
        {
            var node = new UnixFsNode
            {
                Name = "bar",
                Id = "Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD",
                IsDirectory = true,
                Size = 10,
                DagSize = 16
            };
            var link = node.ToLink("foo");
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.DagSize, link.Size);
            Assert.Equal("foo", link.Name);

            link = node.ToLink();
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.DagSize, link.Size);
            Assert.Equal("bar", link.Name);
        }
    }
}
