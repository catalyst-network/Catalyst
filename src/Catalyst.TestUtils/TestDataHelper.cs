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

using System.Linq;
using System.Net;
using System.Text;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;

namespace Catalyst.TestUtils
{
    public static class TestDataHelper
    {
        /// <summary>
        /// Append padding to data that needs to be a fixed length, useful for testing public key length in unit tests
        /// </summary>
        /// <param name="data">The data we are going to add padding to</param>
        /// <param name="expectedLength">The expected length we want the data to be</param>
        /// <param name="padding">The padding char/string to append to the data</param>
        /// <returns>The modified data</returns>
        public static string AppendPadding(string data, int expectedLength, char padding)
        {
            var stringBuilder = new StringBuilder(data);
            var paddingCount = expectedLength - data.Length;
            for (var i = 0; i < paddingCount; i++)
            {
                stringBuilder.Append(padding);
            }
            return stringBuilder.ToString();
        }
    }
}
