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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MultiFormats;

namespace Lib.P2P.Tests.Cryptography
{
    [TestClass]
    public class PreSharedKeyTest
    {
        [TestMethod]
        public void Defaults()
        {
            var psk = new PreSharedKey();
            Assert.IsNull(psk.Value);
            Assert.AreEqual(0, psk.Length);
        }

        [TestMethod]
        public void LengthInBits()
        {
            var psk = new PreSharedKey {Value = new byte[] {1, 2}};
            Assert.AreEqual(16, psk.Length);
        }

        [TestMethod]
        public void Generate()
        {
            var psk = new PreSharedKey().Generate();
            Assert.IsNotNull(psk.Value);
            Assert.AreEqual(256, psk.Length);
        }

        [TestMethod]
        public void Export_Base16()
        {
            var psk1 = new PreSharedKey().Generate();
            var s = new StringWriter();
            psk1.Export(s, "base16");

            var psk2 = new PreSharedKey();
            psk2.Import(new StringReader(s.ToString()));
            CollectionAssert.AreEqual(psk1.Value, psk2.Value);
        }

        [TestMethod]
        public void Export_Base64()
        {
            var psk1 = new PreSharedKey().Generate();
            var s = new StringWriter();
            psk1.Export(s, "base64");

            var psk2 = new PreSharedKey();
            psk2.Import(new StringReader(s.ToString()));
            CollectionAssert.AreEqual(psk1.Value, psk2.Value);
        }

        [TestMethod]
        public void Export_Base16_is_default()
        {
            var psk = new PreSharedKey().Generate();
            var s1 = new StringWriter();
            var s2 = new StringWriter();
            psk.Export(s1);
            psk.Export(s2, "base16");
            Assert.AreEqual(s1.ToString(), s2.ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Export_BadBase()
        {
            var psk = new PreSharedKey().Generate();
            var s = new StringWriter();
            psk.Export(s, "bad");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Import_BadCodec()
        {
            var s = new StringReader("/bad/codec/");
            new PreSharedKey().Import(s);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Import_BadBase()
        {
            var s = new StringReader("/key/swarm/psk/1.0.0/\n/base128/");
            new PreSharedKey().Import(s);
        }

        /// <summary>
        ///   A key generated with
        ///     > npm install ipfs-swarm-key-gen -g
        ///     > node-ipfs-swarm-key-gen
        /// </summary>
        [TestMethod]
        public void Import_JS_Generated()
        {
            var key = "/key/swarm/psk/1.0.0/\n"
              + "/base16/\n"
              + "e8d6d31e8e02000010d7d31e8e020000f0d1fc609300000078f0d31e8e020000";
            var psk2 = new PreSharedKey();
            psk2.Import(new StringReader(key));

            var expected = "e8d6d31e8e02000010d7d31e8e020000f0d1fc609300000078f0d31e8e020000".ToHexBuffer();
            CollectionAssert.AreEqual(expected, psk2.Value);
        }

        [TestMethod]
        public void Fingerprint()
        {
            var key = new PreSharedKey
            {
                Value = "e8d6d31e8e02000010d7d31e8e020000f0d1fc609300000078f0d31e8e020000".ToHexBuffer()
            };
            var expected = "56a19299c05df1f2bb0e1d466002b6d9";
            Assert.AreEqual(expected, key.Fingerprint().ToHexString());
        }
    }
}
