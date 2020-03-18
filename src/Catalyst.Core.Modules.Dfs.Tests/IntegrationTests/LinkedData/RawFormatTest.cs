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

using System.Text;
using Catalyst.Core.Modules.Dfs.LinkedData;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.LinkedData
{
    public class RawFormatTest
    {
        private ILinkedDataFormat formatter = new RawFormat();

        [Fact]
        public void Empty()
        {
            var data = new byte[0];

            var cbor = formatter.Deserialise(data);
            Assert.Equal(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            Assert.Equal(data, data1);
        }

        [Fact]
        public void Data()
        {
            var data = Encoding.UTF8.GetBytes("abc");

            var cbor = formatter.Deserialise(data);
            Assert.Equal(data, cbor["data"].GetByteString());

            var data1 = formatter.Serialize(cbor);
            Assert.Equal(data, data1);
        }
    }
}
