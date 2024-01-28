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
using System.IO;
using System.Text;
using Google.Protobuf;
using MultiFormats;
using Newtonsoft.Json;

namespace Lib.P2P.Tests
{
    public class CidTest
    {
        [Test]
        public void ToString_Default()
        {
            var cid = new Cid {Hash = new MultiHash("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V")};
            Assert.Equals("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", cid.ToString());

            cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            Assert.Equals(
                "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67",
                cid.ToString());
        }

        [Test]
        public void ToString_L()
        {
            var cid = new Cid {Hash = new MultiHash("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V")};
            Assert.Equals("base58btc cidv0 dag-pb sha2-256 QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V",
                cid.ToString("L"));

            cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            Assert.Equals(
                "base58btc cidv1 dag-pb sha2-512 8Vx9QNCcSt39anEamkkSaNw5rDHQ7yuadq7ihZed477qQNXxYr3HReMamd1Q2EnUeL4oNtVAmNw1frEhEN1aoqFuKD",
                cid.ToString("L"));
        }

        [Test]
        public void ToString_G()
        {
            var cid = new Cid {Hash = new MultiHash("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V")};
            Assert.Equals("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", cid.ToString("G"));

            cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            Assert.Equals(
                "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67",
                cid.ToString("G"));
        }

        [Test]
        public void ToString_InvalidFormat()
        {
            var cid = new Cid {Hash = new MultiHash("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V")};
            ExceptionAssert.Throws<FormatException>(() => cid.ToString("?"));
        }

        [Test]
        public void MultiHash_is_Cid_V0()
        {
            var mh = new MultiHash("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V");
            Cid cid = mh;
            Assert.Equals(0, cid.Version);
            Assert.Equals("dag-pb", cid.ContentType);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals(mh, cid.Hash);
        }

        [Test]
        public void MultiHash_is_Cid_V1()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var mh = MultiHash.ComputeHash(hello, "sha2-512");
            Cid cid = mh;
            Assert.Equals(1, cid.Version);
            Assert.Equals("dag-pb", cid.ContentType);
            Assert.Equals("base32", cid.Encoding);
            Assert.Equals(mh, cid.Hash);
        }

        [Test]
        public void Encode_V0()
        {
            var hash = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";
            Cid cid = new MultiHash(hash);
            Assert.Equals(hash, cid.Encode());
            Assert.Equals(0, cid.Version);

            cid = new Cid
            {
                ContentType = "dag-pb",
                Encoding = "base58btc",
                Hash = hash
            };
            Assert.Equals(hash, cid.Encode());
            Assert.Equals(0, cid.Version);
        }

        [Test]
        public void Encode_V1()
        {
            var cid = new Cid
            {
                Version = 1,
                ContentType = "raw",
                Encoding = "base58btc",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            Assert.Equals("zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn", cid.Encode());

            cid = new Cid
            {
                ContentType = "raw",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            Assert.Equals(1, cid.Version);
            Assert.Equals("base32", cid.Encoding);
            Assert.Equals("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e", cid.Encode());
        }

        [Test]
        public void Encode_Upgrade_to_V1_ContentType()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            Assert.Equals(1, cid.Version);
            Assert.Equals("base32", cid.Encoding);
            Assert.Equals("bafkreifzjut3te2nhyekklss27nh3k72ysco7y32koao5eei66wof36n5e", cid.Encode());
        }

        [Test]
        public void Encode_Upgrade_to_V1_Encoding()
        {
            var cid = new Cid
            {
                Encoding = "base64",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            Assert.Equals(1, cid.Version);
            Assert.Equals("mAXASILlNJ7mTTT4IpS5S19p9q/rEhO/jelOA7pCI96zi783p", cid.Encode());
        }

        [Test]
        public void Encode_Upgrade_to_V1_Hash()
        {
            var hello = Encoding.UTF8.GetBytes("Hello, world.");
            var mh = MultiHash.ComputeHash(hello, "sha2-512");
            var cid = new Cid
            {
                Hash = mh
            };
            Assert.Equals(1, cid.Version);
            Assert.Equals("base32", cid.Encoding);
            Assert.Equals(
                "bafybgqfnbq34ghljwmk7hka7cpem3zybbffnsfzfxinq3qyztsuxcntbxaua23xx42hrgptcchrolkndcucelv3pc4eoarjbwdxagtylboxsm",
                cid.Encode());
        }

        [Test]
        public void Encode_V1_Invalid_ContentType()
        {
            var cid = new Cid
            {
                Version = 1,
                ContentType = "unknown",
                Encoding = "base58btc",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
        }

        [Test]
        public void Encode_V1_Invalid_Encoding()
        {
            var cid = new Cid
            {
                Version = 1,
                ContentType = "raw",
                Encoding = "unknown",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            Assert.Throws<KeyNotFoundException>(() => cid.Encode());
        }

        [Test]
        public void Decode_V0()
        {
            var hash = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";
            var cid = Cid.Decode(hash);
            Assert.Equals(0, cid.Version);
            Assert.Equals("dag-pb", cid.ContentType);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals(hash, cid.Encode());
        }

        [Test]
        public void Decode_V0_Invalid()
        {
            var hash = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39?";
            Assert.Throws<FormatException>(() => Cid.Decode(hash));
        }

        [Test]
        public void Decode_Invalid_Version()
        {
            var cid = new Cid
            {
                Version = 32767,
                ContentType = "raw",
                Encoding = "base58btc",
                Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4"
            };
            var s = cid.Encode();
            Assert.Throws<FormatException>(() => Cid.Decode(s));
        }

        [Test]
        public void Decode_V1()
        {
            var id = "zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn";
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var cid = Cid.Decode(id);
            Assert.Equals(1, cid.Version);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals("raw", cid.ContentType);
            Assert.Equals(hash, cid.Hash);
        }

        [Test]
        public void Decode_V1_Unknown_ContentType()
        {
            var id = "zJAFhtPN28kqMxDkZawWCCL52BzaiymqFgX3LA7XzkNRMNAN1T1J";
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var cid = Cid.Decode(id);
            Assert.Equals(1, cid.Version);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals("codec-32767", cid.ContentType);
            Assert.Equals(hash, cid.Hash);
        }

        [Test]
        public void Decode_V1_Invalid_MultiBase_String()
        {
            var id = "zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDX?";
            Assert.Throws<FormatException>(() => Cid.Decode(id));
        }

        [Test]
        public void Decode_V1_Invalid_MultiBase_Code()
        {
            var id = "?";
            Assert.Throws<FormatException>(() => Cid.Decode(id));
        }

        [Test]
        public void Value_Equality()
        {
            var a0 = Cid.Decode("zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn");
            var a1 = Cid.Decode("zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn");
            var b = Cid.Decode("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L5");
            Cid c = null;
            Cid d = null;

            Assert.That(c == d, Is.True);
            Assert.That(c == b, Is.False);
            Assert.That(b == c, Is.False);

            Assert.That(c != d, Is.False);
            Assert.That(c != b, Is.True);
            Assert.That(b != c, Is.True);

#pragma warning disable 1718
            Assert.That(a0 == a0, Is.True);
            Assert.That(a0 == a1, Is.True);
            Assert.That(a0 == b, Is.False);

            Assert.That(a0 != a0, Is.False);
            Assert.That(a0 != a1, Is.False);
            Assert.That(a0 != b, Is.True);

            Assert.That(a0.Equals(a0), Is.True);
            Assert.That(a0.Equals(a1), Is.True);
            Assert.That(a0.Equals(b), Is.False);

            Assert.Equals(a0, a0);
            Assert.Equals(a0, a1);
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.Equals(a0, a0);
            Assert.Equals(a0, a1);
            Assert.That(a0, Is.Not.EqualTo(b));

            Assert.Equals(a0.GetHashCode(), a0.GetHashCode());
            Assert.Equals(a0.GetHashCode(), a1.GetHashCode());
            Assert.That(a0.GetHashCode(), Is.Not.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Implicit_Conversion_From_V0_String()
        {
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            Cid cid = hash;
            Assert.Equals(0, cid.Version);
            Assert.Equals("dag-pb", cid.ContentType);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals(hash, cid.Encode());
        }

        [Test]
        public void Implicit_Conversion_From_V1_String()
        {
            var id = "zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn";
            var hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            Cid cid = id;
            Assert.Equals(1, cid.Version);
            Assert.Equals("base58btc", cid.Encoding);
            Assert.Equals("raw", cid.ContentType);
            Assert.Equals(hash, cid.Hash);
        }

        [Test]
        public void Implicit_Conversion_To_String()
        {
            var id = "zb2rhj7crUKTQYRGCRATFaQ6YFLTde2YzdqbbhAASkL9uRDXn";
            Cid cid = id;
            string s = cid;
            Assert.Equals(id, s);
        }

        [Test]
        public void Streaming_V0()
        {
            Cid cid = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var stream = new MemoryStream();
            cid.Write(stream);
            stream.Position = 0;
            var clone = Cid.Read(stream);
            Assert.Equals(cid.Version, clone.Version);
            Assert.Equals(cid.ContentType, clone.ContentType);
            Assert.Equals(cid.Hash, clone.Hash);
        }

        [Test]
        public void Streaming_V1()
        {
            Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            var stream = new MemoryStream();
            cid.Write(stream);
            stream.Position = 0;
            var clone = Cid.Read(stream);
            Assert.Equals(cid.Version, clone.Version);
            Assert.Equals(cid.ContentType, clone.ContentType);
            Assert.Equals(cid.Hash, clone.Hash);
        }

        [Test]
        public void Protobuf_V0()
        {
            Cid cid = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var stream = new MemoryStream();
            var cos = new CodedOutputStream(stream);
            cid.Write(cos);
            cos.Flush();
            stream.Position = 0;
            var cis = new CodedInputStream(stream);
            var clone = Cid.Read(cis);
            Assert.Equals(cid.Version, clone.Version);
            Assert.Equals(cid.ContentType, clone.ContentType);
            Assert.Equals(cid.Hash, clone.Hash);
        }

        [Test]
        public void Protobuf_V1()
        {
            Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            var stream = new MemoryStream();
            var cos = new CodedOutputStream(stream);
            cid.Write(cos);
            cos.Flush();
            stream.Position = 0;
            var cis = new CodedInputStream(stream);
            var clone = Cid.Read(cis);
            Assert.Equals(cid.Version, clone.Version);
            Assert.Equals(cid.ContentType, clone.ContentType);
            Assert.Equals(cid.Hash, clone.Hash);
        }

        [Test]
        public void Immutable()
        {
            Cid cid = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            Assert.Equals("QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4", cid.Encode());
            ExceptionAssert.Throws<NotSupportedException>(() => cid.ContentType = "dag-cbor");
            ExceptionAssert.Throws<NotSupportedException>(() => cid.Encoding = "base64");
            ExceptionAssert.Throws<NotSupportedException>(() =>
                cid.Hash = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L5");
            ExceptionAssert.Throws<NotSupportedException>(() => cid.Version = 0);
        }

        private sealed class CidAndX
        {
            public Cid Cid;
            public int X;
        }

        [Test]
        public void JsonSerialization()
        {
            Cid a = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4";
            var json = JsonConvert.SerializeObject(a);
            Assert.Equals($"\"{a.Encode()}\"", json);
            var b = JsonConvert.DeserializeObject<Cid>(json);
            Assert.Equals(a, b);

            a = null;
            json = JsonConvert.SerializeObject(a);
            b = JsonConvert.DeserializeObject<Cid>(json);
            Assert.That(b, Is.Null);

            var x = new CidAndX {Cid = "QmaozNR7DZHQK1ZcU9p7QdrshMvXqWK6gpu5rmrkPdT3L4", X = 42};
            json = JsonConvert.SerializeObject(x);
            var y = JsonConvert.DeserializeObject<CidAndX>(json);
            Assert.Equals(x.Cid, y.Cid);
            Assert.Equals(x.X, y.X);

            x.Cid = null;
            json = JsonConvert.SerializeObject(x);
            y = JsonConvert.DeserializeObject<CidAndX>(json);
            Assert.Equals(x.Cid, y.Cid);
            Assert.Equals(x.X, y.X);
        }

        [Test]
        public void ByteArrays_V1()
        {
            Cid cid = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";
            var buffer = cid.ToArray();
            var clone = Cid.Read(buffer);
            Assert.Equals(cid.Version, clone.Version);
            Assert.Equals(cid.ContentType, clone.ContentType);
            Assert.Equals(cid.Hash.Algorithm.Name, clone.Hash.Algorithm.Name);
            Assert.Equals(cid.Hash, clone.Hash);
        }

        [Test]
        public void ByteArrays_V0()
        {
            var buffer = "1220a4edf38611d7d4a2d3ff2d97f88a7256eba31b57982f803b4de7bbeb0343c37b".ToHexBuffer();
            var cid = Cid.Read(buffer);
            Assert.Equals(0, cid.Version);
            Assert.Equals("dag-pb", cid.ContentType);
            Assert.Equals("QmZSU1xNFsBtCnzK2Nk9N4bAxQiVNdmugU9DQDE3ntkTpe", cid.Hash.ToString());

            var clone = cid.ToArray();
            Assert.That(buffer, Is.EquivalentTo(clone));
        }
    }
}
