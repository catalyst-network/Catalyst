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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Extensions
{
    public static class EnumerableExtensionTests
    {
        [Fact]
        public static void RandomElementReturnsCorrectTypeOfString()
        {
            var randomList = new List<string>();
            randomList.Add(CorrelationId.GenerateCorrelationId().Id.ToString());
            var returnedElement = randomList.RandomElement();

            returnedElement.Should().BeOfType<string>();
        }

        [Fact]
        public static void RandomElementReturnsCorrectTypeOfBool()
        {
            var randomList = new List<bool> {false};
            var returnedElement = randomList.RandomElement();

            returnedElement.Should().BeFalse();
        }

        [Fact]
        public static void GetARandomElement()
        {
            var randomList = new List<string>();
            var checkElementList = new List<string>();

            for (var i = 0; i < 50; i++)
            {
                randomList.Add(CorrelationId.GenerateCorrelationId().Id.ToString());
            }

            for (var i = 0; i < 5; i++)
            {
                checkElementList.Add(randomList.RandomElement());
            }

            checkElementList.Distinct().Count().Should().BeGreaterThan(1);
        }
    }
}
