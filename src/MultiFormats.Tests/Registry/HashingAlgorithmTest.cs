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
using System.Collections.Generic;
using System.Linq;
using MultiFormats.Registry;

namespace MultiFormats.Tests.Registry
{
    public class HashingAlgorithmTest
    {
        [Test]
        public void GetHasher()
        {
            using (var hasher = HashingAlgorithm.GetAlgorithm("sha3-256"))
            {
                Assert.That(hasher, Is.Not.Null);
                var input = new byte[]
                {
                    0xe9
                };
                var expected = "f0d04dd1e6cfc29a4460d521796852f25d9ef8d28b44ee91ff5b759d72c1e6d6".ToHexBuffer();

                var actual = hasher.ComputeHash(input);
                Assert.That(expected, Is.EquivalentTo(actual));
            }
        }

        [Test]
        public void GetHasher_Unknown()
        {
            ExceptionAssert.Throws<KeyNotFoundException>(() => HashingAlgorithm.GetAlgorithm("unknown"));
        }

        [Test]
        public void GetMetadata()
        {
            var info = HashingAlgorithm.GetAlgorithmMetadata("sha3-256");
            Assert.That(info, Is.Not.Null);
            Assert.That("sha3-256", Is.EqualTo(info.Name));
            Assert.That(0x16, Is.EqualTo(info.Code));
            Assert.That(256 / 8, Is.EqualTo(info.DigestSize));
            Assert.That(info.Hasher, Is.Not.Null);
        }

        [Test]
        public void GetMetadata_Unknown()
        {
            ExceptionAssert.Throws<KeyNotFoundException>(() => HashingAlgorithm.GetAlgorithmMetadata("unknown"));
        }

        [Test]
        public void GetMetadata_Alias()
        {
            var info = HashingAlgorithm.GetAlgorithmMetadata("id");
            Assert.That(info, Is.Not.Null);
            Assert.That("identity", Is.EqualTo(info.Name));
            Assert.That(0, Is.EqualTo(info.Code));
            Assert.That(0, Is.EqualTo(info.DigestSize));
            Assert.That(info.Hasher, Is.Not.Null);
        }

        [Test]
        public void HashingAlgorithm_Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register(null, 1, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register("", 1, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register("   ", 1, 1));
        }

        [Test]
        public void HashingAlgorithm_Name_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.Register("sha1", 0x11, 1));
        }

        [Test]
        public void HashingAlgorithm_Number_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.Register("sha1-x", 0x11, 1));
        }

        [Test]
        public void HashingAlgorithms_Are_Enumerable() { Assert.That(5 <= HashingAlgorithm.All.Count(), Is.True); }

        [Test]
        public void HashingAlgorithm_Bad_Alias()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias(null, "sha1"));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias("", "sha1"));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias("   ", "sha1"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("id", "identity"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Target_Does_Not_Exist()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("foo", "sha1-x"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Target_Is_Bad()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("foo", "  "));
        }
    }
}
