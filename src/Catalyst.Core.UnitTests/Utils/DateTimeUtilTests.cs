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
using Catalyst.Core.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
{
    public sealed class DateTimeUtilTests
    {
        [Fact]
        public void ExponentialTimeSpan_Should_Return_Exponential_TimeSpan()
        {
            var maxTimeSpan = TimeSpan.MaxValue;
            var retryCount = 0;
            var milliseconds = Math.Pow(2, retryCount);

            while (milliseconds < maxTimeSpan.TotalMilliseconds)
            {
                var result = DateTimeUtil.GetExponentialTimeSpan(retryCount);
                var target = TimeSpan.FromMilliseconds(milliseconds);
                var comparison = TimeSpan.Compare(result, target);
                comparison.Should().Be(0);

                retryCount++;
                milliseconds = Math.Pow(2, retryCount);
            }
        }
    }
}
