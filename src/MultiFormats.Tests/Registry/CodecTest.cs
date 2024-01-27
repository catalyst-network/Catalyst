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
using System.Linq;
using MultiFormats.Registry;

namespace MultiFormats.Tests.Registry
{
    public class CodecTest
    {
        [Test]
        public void Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register(null, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register("", 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register("   ", 1));
        }

        [Test]
        public void Name_Already_Exists() { ExceptionAssert.Throws<ArgumentException>(() => Codec.Register("raw", 1)); }

        [Test]
        public void Code_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => Codec.Register("raw-x", 0x55));
        }

        [Test]
        public void Algorithms_Are_Enumerable() { Assert.That(0, Is.Not.EqualTo(Codec.All.Count())); }

        [Test]
        public void Register()
        {
            var codec = Codec.Register("something-new", 0x0bad);
            try
            {
                Assert.Equals("something-new", codec.Name);
                Assert.Equals("something-new", codec.ToString());
                Assert.Equals(0x0bad, codec.Code);
            }
            finally
            {
                Codec.Deregister(codec);
            }
        }
    }
}
