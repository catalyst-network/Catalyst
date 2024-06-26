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

using Catalyst.Abstractions.Dfs.CoreApi;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class ObjectStatTest
    {
        [Test]
        public void Properties()
        {
            var stat = new ObjectStat
            {
                BlockSize = 1,
                CumulativeSize = 2,
                DataSize = 3,
                LinkCount = 4,
                LinkSize = 5
            };
            Assert.That(stat.BlockSize, Is.EqualTo(1));
            Assert.That(stat.CumulativeSize, Is.EqualTo(2));
            Assert.That(stat.DataSize, Is.EqualTo(3));
            Assert.That(stat.LinkCount, Is.EqualTo(4));
            Assert.That(stat.LinkSize, Is.EqualTo(5));
        }
    }
}
