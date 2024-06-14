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
using System.IO;
using Lib.P2P.Cryptography;
using Lib.P2P.PubSub;
using Lib.P2P.SecureCommunication;

namespace Lib.P2P.Tests.SecureCommunication
{
    public class Psk1StreamTest
    {
        [Test]
        public void BadKeyLength()
        {
            var psk = new PreSharedKey();
            Assert.That(() => new Psk1Stream(Stream.Null, psk), Throws.Exception);
        }

        [Test]
        public void FirstWriteSendsNonce()
        {
            var psk = new PreSharedKey().Generate();

            var insecure = new MemoryStream();
            var secure = new Psk1Stream(insecure, psk);
            secure.WriteByte(0x10);
            Assert.That(insecure.Length, Is.EqualTo(24 + 1));

            insecure = new MemoryStream();
            secure = new Psk1Stream(insecure, psk);
            secure.Write(new byte[10], 0, 10);
            Assert.That(insecure.Length, Is.EqualTo(24 + 10));

            insecure = new MemoryStream();
            secure = new Psk1Stream(insecure, psk);
            secure.WriteAsync(new byte[12], 0, 12).Wait();
            Assert.That(insecure.Length, Is.EqualTo(24 + 12));
        }

        [Test]
        public void Roundtrip()
        {
            var psk = new PreSharedKey().Generate();
            var plain = new byte[] {1, 2, 3};
            var plain1 = new byte[3];
            var plain2 = new byte[3];

            var insecure = new MemoryStream();
            var secure = new Psk1Stream(insecure, psk);
            secure.Write(plain, 0, plain.Length);
            secure.Flush();

            insecure.Position = 0;
            secure = new Psk1Stream(insecure, psk);
            secure.Read(plain1, 0, plain1.Length);
            Assert.That(plain, Is.EquivalentTo(plain1));

            insecure.Position = 0;
            secure = new Psk1Stream(insecure, psk);
            secure.ReadAsync(plain2, 0, plain2.Length).Wait();
            Assert.That(plain, Is.EquivalentTo(plain2));
        }

        [Test]
        public void ReadingInvalidNonce()
        {
            var psk = new PreSharedKey().Generate();
            var secure = new Psk1Stream(Stream.Null, psk);
            Assert.That(() => secure.ReadByte(), Throws.TypeOf<EndOfStreamException>());
        }
    }
}
