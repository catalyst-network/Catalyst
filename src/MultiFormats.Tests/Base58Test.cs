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
using System.Text;

namespace MultiFormats.Tests
{
    public class Base58Test
    {
        [Test]
        public void Encode()
        {
            Assert.That("jo91waLQA1NNeBmZKUF", Is.EqualTo(Base58.Encode(Encoding.UTF8.GetBytes("this is a test"))));
            Assert.That("jo91waLQA1NNeBmZKUF", Is.EqualTo(Encoding.UTF8.GetBytes("this is a test").ToBase58()));
        }

        [Test]
        public void Decode()
        {
            Assert.That("this is a test", Is.EqualTo(Encoding.UTF8.GetString(Base58.Decode("jo91waLQA1NNeBmZKUF"))));
            Assert.That("this is a test", Is.EqualTo(Encoding.UTF8.GetString("jo91waLQA1NNeBmZKUF".FromBase58())));
        }

        /// <summary>
        ///    C# version of base58Test in <see href="https://github.com/ipfs/java-ipfs-api/blob/master/test/org/ipfs/Test.java"/>
        /// </summary>
        [Test]
        public void Java()
        {
            var input = "QmPZ9gcCEpqKTo6aq61g2nXGUhM4iCL3ewB6LDXZCtioEB";
            var output = Base58.Decode(input);
            var encoded = Base58.Encode(output);
            Assert.That(input, Is.EqualTo(encoded));
        }

        [Test]
        public void Decode_Bad()
        {
            ExceptionAssert.Throws<ArgumentException>(() => Base58.Decode("jo91waLQA1NNeBmZKUF=="));
        }

        [Test]
        public void Zero()
        {
            Assert.That("1111111", Is.EqualTo(Base58.Encode(new byte[7])));
            Assert.That(7, Is.EqualTo(Base58.Decode("1111111").Length));
            Assert.That(Base58.Decode("1111111").All(b => b == 0), Is.True);
        }
    }
}
