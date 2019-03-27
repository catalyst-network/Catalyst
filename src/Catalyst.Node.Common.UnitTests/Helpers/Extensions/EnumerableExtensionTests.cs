/*
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

using System;
using System.Collections.Generic;
using Catalyst.Node.Common.Helpers.Extensions;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers.Extensions
{
    public class EnumerableExtensionTests
    {
        [Fact]
        public void GetARandomElement()
        {
            var randomList = new List<string>();
            
            for (int i = 0; i < 5; i++)
            {
                randomList.Add(Guid.NewGuid().ToString());
            }

            var randomElement1 = randomList.RandomElement();

            randomElement1.Should().NotBeNullOrEmpty();
            randomList.Should().Contain(randomElement1);

            var randomElement2 = randomList.RandomElement();

            randomElement2.Should().NotBeNullOrEmpty();
            randomList.Should().Contain(randomElement2);
        }
    }
}