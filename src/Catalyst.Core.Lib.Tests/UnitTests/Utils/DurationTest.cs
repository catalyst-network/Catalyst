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
using Catalyst.Core.Lib.Util;
using Catalyst.TestUtils;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Utils
{
    public class DurationTest
    {
        [Test]
        public void Parsing_Examples()
        {
            Assert.Equals(TimeSpan.FromMilliseconds(300), Duration.Parse("300ms"));
            Assert.Equals(TimeSpan.FromHours(-1.5), Duration.Parse("-1.5h"));
            Assert.Equals(new TimeSpan(2, 45, 0), Duration.Parse("2h45m"));
            Assert.Equals(new TimeSpan(0, 1, 0) + TimeSpan.FromSeconds(4.483878032),
                Duration.Parse("1m4.483878032s"));
        }

        [Test]
        public void Parsing_Zero()
        {
            Assert.Equals(TimeSpan.Zero, Duration.Parse("0s"));
            Assert.Equals(TimeSpan.Zero, Duration.Parse("0µs"));
            Assert.Equals(TimeSpan.Zero, Duration.Parse("0ns"));
            Assert.Equals(TimeSpan.Zero, Duration.Parse("n/a"));
            Assert.Equals(TimeSpan.Zero, Duration.Parse("unknown"));
            Assert.Equals(TimeSpan.Zero, Duration.Parse(""));
        }

        [Test]
        public void Parsing_Negative() { Assert.Equals(TimeSpan.FromHours(-2), Duration.Parse("-1.5h30m")); }

        [Test]
        public void Parsing_Submilliseconds()
        {
            // Note: Equals of TimeSpan is 100ns, e.g. 1 tick.
            Assert.Equals(TimeSpan.FromTicks(2), Duration.Parse("200ns"));
            Assert.Equals(TimeSpan.FromTicks(2000), Duration.Parse("200us"));
            Assert.Equals(TimeSpan.FromTicks(2000), Duration.Parse("200µs"));
        }

        [Test]
        public void Parsing_MissingNumber()
        {
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = Duration.Parse("s");
            });
        }

        [Test]
        public void Parsing_MissingUnit()
        {
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = Duration.Parse("1");
            }, "Missing IPFS duration unit.");
        }

        [Test]
        public void Parsing_InvalidUnit()
        {
            ExceptionAssert.Throws<FormatException>(() =>
            {
                var _ = Duration.Parse("1jiffy");
            }, "Unknown IPFS duration unit 'jiffy'.");
        }

        [Test]
        public void Stringify()
        {
            Assert.Equals("0s", Duration.Stringify(TimeSpan.Zero));
            Assert.Equals("n/a", Duration.Stringify(TimeSpan.Zero, "n/a"));

            Assert.Equals("2h", Duration.Stringify(new TimeSpan(2, 0, 0)));
            Assert.Equals("3m", Duration.Stringify(new TimeSpan(0, 3, 0)));
            Assert.Equals("4s", Duration.Stringify(new TimeSpan(0, 0, 4)));
            Assert.Equals("5ms", Duration.Stringify(new TimeSpan(0, 0, 0, 0, 5)));
            Assert.Equals("2h4s", Duration.Stringify(new TimeSpan(2, 0, 4)));
            Assert.Equals("26h3m4s5ms", Duration.Stringify(new TimeSpan(1, 2, 3, 4, 5)));

            Assert.Equals("-48h", Duration.Stringify(TimeSpan.FromDays(-2)));
            Assert.Equals("-2h", Duration.Stringify(TimeSpan.FromHours(-2)));
            Assert.Equals("-1h30m", Duration.Stringify(TimeSpan.FromHours(-1.5)));

            Assert.Equals("200ns", Duration.Stringify(TimeSpan.FromTicks(2)));
            Assert.Equals("200us", Duration.Stringify(TimeSpan.FromTicks(2000)));
            Assert.Equals("200us300ns", Duration.Stringify(TimeSpan.FromTicks(2003)));
        }
    }
}
