#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats.Registry;

namespace MultiFormats.Tests.Registry
{
    [TestClass]
    public class CodecTest
    {
        [TestMethod]
        public void Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register(null, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register("", 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => Codec.Register("   ", 1));
        }

        [TestMethod]
        public void Name_Already_Exists() { ExceptionAssert.Throws<ArgumentException>(() => Codec.Register("raw", 1)); }

        [TestMethod]
        public void Code_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => Codec.Register("raw-x", 0x55));
        }

        [TestMethod]
        public void Algorithms_Are_Enumerable() { Assert.AreNotEqual(0, Codec.All.Count()); }

        [TestMethod]
        public void Register()
        {
            var codec = Codec.Register("something-new", 0x0bad);
            try
            {
                Assert.AreEqual("something-new", codec.Name);
                Assert.AreEqual("something-new", codec.ToString());
                Assert.AreEqual(0x0bad, codec.Code);
            }
            finally
            {
                Codec.Deregister(codec);
            }
        }
    }
}
