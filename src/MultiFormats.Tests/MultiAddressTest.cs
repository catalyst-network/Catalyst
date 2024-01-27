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
using System.Net;
using Newtonsoft.Json;

namespace MultiFormats.Tests
{
    public class MultiAddressTest
    {
        private const string Somewhere =
            "/ip4/10.1.10.10/tcp/29087/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC";

        private const string Nowhere = "/ip4/10.1.10.11/tcp/29087/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC";

        [Test]
        public void Parsing()
        {
            var a = new MultiAddress(Somewhere);
            Assert.Equals(3, a.Protocols.Count);
            Assert.Equals("ip4", a.Protocols[0].Name);
            Assert.Equals("10.1.10.10", a.Protocols[0].Value);
            Assert.Equals("tcp", a.Protocols[1].Name);
            Assert.Equals("29087", a.Protocols[1].Value);
            Assert.Equals("ipfs", a.Protocols[2].Name);
            Assert.Equals("QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC", a.Protocols[2].Value);

            Assert.Equals(0, new MultiAddress((string)null).Protocols.Count);
            Assert.Equals(0, new MultiAddress("").Protocols.Count);
            Assert.Equals(0, new MultiAddress("  ").Protocols.Count);
        }

        [Test]
        public void Unknown_Protocol_Name()
        {
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/foobar/123"));
        }

        [Test]
        public void Missing_Protocol_Name() { ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/")); }

        [Test]
        public new void ToString() { Assert.Equals(Somewhere, new MultiAddress(Somewhere).ToString()); }

        [Test]
        public void Value_Equality()
        {
            var a0 = new MultiAddress(Somewhere);
            var a1 = new MultiAddress(Somewhere);
            var b = new MultiAddress(Nowhere);
            MultiAddress c = null;
            MultiAddress d = null;

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

#pragma warning disable 1718
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
        public void Bad_Port()
        {
            var tcp = new MultiAddress("/tcp/65535");
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/tcp/x"));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/tcp/65536"));

            var udp = new MultiAddress("/udp/65535");
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/upd/x"));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/udp/65536"));
        }

        [Test]
        public void Bad_IPAddress()
        {
            var ipv4 = new MultiAddress("/ip4/127.0.0.1");
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip4/x"));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip4/127."));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip4/::1"));

            var ipv6 = new MultiAddress("/ip6/::1");
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip6/x"));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip6/03:"));
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("/ip6/127.0.0.1"));
        }

        [Test]
        public void Bad_Onion_MultiAdress()
        {
            var badCases = new[]
            {
                "/onion/9imaq4ygg2iegci7:80",
                "/onion/aaimaq4ygg2iegci7:80",
                "/onion/timaq4ygg2iegci7:0",
                "/onion/timaq4ygg2iegci7:-1",
                "/onion/timaq4ygg2iegci7",
                "/onion/timaq4ygg2iegci@:666",
            };
            foreach (var badCase in badCases)
            {
                ExceptionAssert.Throws<Exception>(() => new MultiAddress(badCase));
            }
        }

        [Test]
        public void RoundTripping()
        {
            var addresses = new[]
            {
                Somewhere,
                "/ip4/1.2.3.4/tcp/80/http",
                "/ip6/3ffe:1900:4545:3:200:f8ff:fe21:67cf/tcp/443/https",
                "/ip6/3ffe:1900:4545:3:200:f8ff:fe21:67cf/udp/8001",
                "/ip6/3ffe:1900:4545:3:200:f8ff:fe21:67cf/sctp/8001",
                "/ip6/3ffe:1900:4545:3:200:f8ff:fe21:67cf/dccp/8001",
                "/ip4/1.2.3.4/tcp/80/ws",
                "/libp2p-webrtc-star/ip4/127.0.0.1/tcp/9090/ws/ipfs/QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC",
                "/ip4/127.0.0.1/tcp/1234/ipfs/QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC",
                "/ip4/1.2.3.4/tcp/80/udt",
                "/ip4/1.2.3.4/tcp/80/utp",
                "/onion/aaimaq4ygg2iegci:80",
                "/onion/timaq4ygg2iegci7:80/http",
                "/p2p-circuit/ipfs/QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC",
                "/dns/ipfs.io",
                "/dns4/ipfs.io",
                "/dns6/ipfs.io",
                "/dns4/wss0.bootstrap.libp2p.io/tcp/443/wss/ipfs/QmZMxNdpMkewiVZLMRxaNxUeZpDUb34pWjZ1kZvsd16Zic",
                "/ip4/127.0.0.0/ipcidr/16",
                "/p2p/QmNnooDu7bfjPFoTZYxMNLWUQJyrVwtbZg5gBMjTezGAJN",
                "/ip4/127.0.0.1/udp/4023/quic",
            };
            foreach (var a in addresses)
            {
                var ma0 = new MultiAddress(a);

                var ms = new MemoryStream();
                ma0.Write(ms);
                ms.Position = 0;
                var ma1 = new MultiAddress(ms);
                Assert.Equals(ma0, ma1);

                var ma2 = new MultiAddress(ma0.ToString());
                Assert.Equals(ma0, ma2);

                var ma3 = new MultiAddress(ma0.ToArray());
                Assert.Equals(ma0, ma3);
            }
        }

        [Test]
        public void Reading_Invalid_Code()
        {
            ExceptionAssert.Throws<InvalidDataException>(() => new MultiAddress(new byte[]
            {
                0x7F
            }));
        }

        [Test]
        public void Reading_Invalid_Text()
        {
            ExceptionAssert.Throws<FormatException>(() => new MultiAddress("tcp/80"));
        }

        [Test]
        public void Implicit_Conversion_From_String()
        {
            MultiAddress a = Somewhere;
            Assert.That(a, Is.TypeOf(typeof(MultiAddress)));
        }

        [Test]
        public void Wire_Formats()
        {
            Assert.Equals(
                new MultiAddress("/ip4/127.0.0.1/udp/1234").ToArray().ToHexString(),
                "047f000001910204d2");
            Assert.Equals(
                new MultiAddress("/ip4/127.0.0.1/udp/1234/ip4/127.0.0.1/tcp/4321").ToArray().ToHexString(),
                "047f000001910204d2047f0000010610e1");
            Assert.Equals(
                new MultiAddress("/ip6/2001:8a0:7ac5:4201:3ac9:86ff:fe31:7095").ToArray().ToHexString(),
                "29200108a07ac542013ac986fffe317095");
            Assert.Equals(
                new MultiAddress("/ipfs/QmcgpsyWgH8Y8ajJz1Cu72KnS5uo2Aa2LpzU7kinSupNKC").ToArray().ToHexString(),
                "a503221220d52ebb89d85b02a284948203a62ff28389c57c9f42beec4ec20db76a68911c0b");
            Assert.Equals(
                new MultiAddress("/ip4/127.0.0.1/udp/1234/utp").ToArray().ToHexString(),
                "047f000001910204d2ae02");
            Assert.Equals(
                new MultiAddress("/onion/aaimaq4ygg2iegci:80").ToArray().ToHexString(),
                "bc030010c0439831b48218480050");
        }

        [Test]
        public void PeerID_With_ipfs()
        {
            var ma = new MultiAddress("/ip4/10.1.10.10/tcp/29087/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            Assert.Equals("QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC", ma.PeerId.ToBase58());
        }

        [Test]
        public void PeerID_With_p2p()
        {
            var ma = new MultiAddress("/ip4/10.1.10.10/tcp/29087/p2p/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            Assert.Equals("QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC", ma.PeerId.ToBase58());
        }

        [Test]
        public void PeerID_ipfs_p2p_are_equal()
        {
            var ipfs = new MultiAddress(
                "/ip4/10.1.10.10/tcp/29087/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            var p2P = new MultiAddress("/ip4/10.1.10.10/tcp/29087/p2p/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            Assert.Equals(ipfs, p2P);

            var p2P1 = new MultiAddress("/ip4/10.1.10.10/tcp/29087/p2p/QmVCSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            Assert.That(p2P, Is.Not.EqualTo(p2P1));

            var p2P2 = new MultiAddress("/p2p/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC");
            Assert.That(p2P, Is.Not.EqualTo(p2P2));
        }

        [Test]
        public void PeerID_Missing()
        {
            var ma = new MultiAddress("/ip4/10.1.10.10/tcp/29087");
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ma.PeerId;
            });
        }

        [Test]
        public void PeerId_IsPresent()
        {
            Assert.That(
                new MultiAddress("/ip4/10.1.10.10/tcp/29087/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC")
                   .HasPeerId, Is.True);
            Assert.That(
                new MultiAddress("/ip4/10.1.10.10/tcp/29087/p2p/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC")
                   .HasPeerId, Is.True);
            Assert.That(new MultiAddress("/ip4/10.1.10.10/tcp/29087").HasPeerId, Is.False);
        }

        [Test]
        public void Cloning()
        {
            var ma1 = new MultiAddress("/ip4/10.1.10.10/tcp/29087");
            var ma2 = ma1.Clone();
            Assert.Equals(ma1, ma2);
            Assert.That(ma1, Is.Not.EqualTo(ma2));
            Assert.That(ma1.Protocols, Is.Not.EqualTo(ma2.Protocols));
            for (var i = 0; i < ma1.Protocols.Count; ++i)
            {
                var p1 = ma1.Protocols[i];
                var p2 = ma2.Protocols[i];
                Assert.Equals(p1.Code, p2.Code);
                Assert.Equals(p1.Name, p2.Name);
                Assert.Equals(p1.Value, p2.Value);
                Assert.That(p1, Is.Not.EqualTo(p2));
            }
        }

        [Test]
        public void Ipv6ScopeId_Ignored()
        {
            var ma1 = new MultiAddress("/ip6/fe80::7573:b0a8:46b0:0bad%17/tcp/4009");
            var ma2 = new MultiAddress("/ip6/fe80::7573:b0a8:46b0:0bad/tcp/4009");
            Assert.Equals(ma2, ma1);
            Assert.Equals(ma2.ToString(), ma1.ToString());
        }

        [Test]
        public void TryCreate_FromString()
        {
            Assert.That(MultiAddress.TryCreate("/ip4/1.2.3.4/tcp/80"), Is.Not.Null);
            Assert.That(MultiAddress.TryCreate("/tcp/alpha"), Is.Null); // bad port
            Assert.That(MultiAddress.TryCreate("/foobar"), Is.Null);    // bad protocol
        }

        [Test]
        public void TryCreate_FromBytes()
        {
            var good = MultiAddress.TryCreate("/ip4/1.2.3.4/tcp/80");
            var good1 = MultiAddress.TryCreate(good.ToArray());
            Assert.Equals(good, good1);

            Assert.That(MultiAddress.TryCreate(new byte[]
            {
                0x7f
            }), Is.Null);
        }

        [Test]
        public void JsonSerialization()
        {
            var a = new MultiAddress("/ip6/fe80::7573:b0a8:46b0:0bad/tcp/4009");
            var json = JsonConvert.SerializeObject(a);
            Assert.Equals($"\"{a}\"", json);
            var b = JsonConvert.DeserializeObject<MultiAddress>(json);
            Assert.Equals(a.ToString(), b.ToString());

            json = JsonConvert.SerializeObject(null);
            b = JsonConvert.DeserializeObject<MultiAddress>(json);
            Assert.That(b, Is.Null);
        }

        [Test]
        public void WithPeerId()
        {
            const string id = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22";
            const string id3 = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE33";

            var ma1 = new MultiAddress("/ip4/127.0.0.1/tcp/4001");
            Assert.Equals($"{ma1}/p2p/{id}", ma1.WithPeerId(id));

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id}");
            Assert.Equals(ma1, ma1.WithPeerId(id));

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/p2p/{id}");
            Assert.Equals(ma1, ma1.WithPeerId(id));

            ExceptionAssert.Throws<Exception>(() =>
            {
                ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id3}");
                Assert.Equals(ma1, ma1.WithPeerId(id));
            });
        }

        [Test]
        public void WithoutPeerId()
        {
            var id = "QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22";

            var ma1 = new MultiAddress("/ip4/127.0.0.1/tcp/4001");
            Assert.Equals(ma1, ma1.WithoutPeerId());

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/ipfs/{id}");
            Assert.Equals("/ip4/127.0.0.1/tcp/4001", ma1.WithoutPeerId());

            ma1 = new MultiAddress($"/ip4/127.0.0.1/tcp/4001/p2p/{id}");
            Assert.Equals("/ip4/127.0.0.1/tcp/4001", ma1.WithoutPeerId());
        }

        [Test]
        public void Alias_Equality()
        {
            var a = new MultiAddress("/ipfs/QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22");
            var b = new MultiAddress("/p2p/QmQusTXc1Z9C1mzxsqC9ZTFXCgSkpBRGgW4Jk2QYHxKE22");

            Assert.Equals(a, b);
            Assert.That(a == b, Is.True);
            Assert.Equals(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_NullValue()
        {
            var a = new MultiAddress(
                "/ip4/139.178.69.3/udp/4001/quic/p2p/QmdGQoGuK3pao6bRDqGSDvux5SFHa4kC2XNFfHFcvcbydY/p2p-circuit/ipfs/QmPJkpfUedzahgVAj6tTUa3DHKVkfTSyvUmnn1USFpiCaF");
            var _ = a.GetHashCode();
        }

        [Test]
        public void FromIpAddress()
        {
            var ma = new MultiAddress(IPAddress.Loopback);
            Assert.Equals("/ip4/127.0.0.1", ma.ToString());

            ma = new MultiAddress(IPAddress.IPv6Loopback);
            Assert.Equals("/ip6/::1", ma.ToString());
        }

        [Test]
        public void FromIpEndpoint()
        {
            var ma = new MultiAddress(new IPEndPoint(IPAddress.Loopback, 4001));
            Assert.Equals("/ip4/127.0.0.1/tcp/4001", ma.ToString());

            ma = new MultiAddress(new IPEndPoint(IPAddress.IPv6Loopback, 4002));
            Assert.Equals("/ip6/::1/tcp/4002", ma.ToString());
        }
    }
}
