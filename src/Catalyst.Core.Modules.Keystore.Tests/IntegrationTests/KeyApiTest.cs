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
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Config;
using Catalyst.TestUtils;
using Lib.P2P;
using Makaretu.Dns;
using NSubstitute;
using Org.BouncyCastle.Crypto.Parameters;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Keystore.Tests.IntegrationTests
{
    public sealed class KeyApiTest : FileSystemBasedTest
    {
        private readonly IKeyStoreService _keyStoreService;

        public KeyApiTest(ITestOutputHelper output) : base(output)
        {
            var dfsOptions = new DfsOptions(new BlockOptions(), new DiscoveryOptions(), new RepositoryOptions(FileSystem, Constants.DfsDataSubDir), Substitute.For<KeyChainOptions>(), Substitute.For<SwarmOptions>(), Substitute.For<IDnsClient>());
            _keyStoreService = new KeyStoreService(dfsOptions);
            _keyStoreService.SetPassphraseAsync(new SecureString()).Wait();
        }

        [Fact]
        public async Task Self_Key_Exists()
        {
            const string name = "self";
            var _ = await _keyStoreService.CreateAsync(name, "ed25519", 0);

            var keys = await _keyStoreService.ListAsync();
            var self = keys.Single(k => k.Name == "self");

            var me = await _keyStoreService.FindKeyByNameAsync("self");
            Assert.Equal("self", self.Name);
            Assert.Equal(me.Id, self.Id);
        }

        [Fact]
        public async Task Export_Import()
        {
            const string name = "self";
            var _ = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            var password = "password".ToCharArray();
            var pem = await _keyStoreService.ExportAsync(name, password);
            Assert.StartsWith("-----BEGIN ENCRYPTED PRIVATE KEY-----", pem);

            var keys = await _keyStoreService.ListAsync();
            var self = keys.Single(k => k.Name == name);

            await _keyStoreService.RemoveAsync("clone");
            var clone = await _keyStoreService.ImportAsync("clone", pem, password);
            Assert.Equal("clone", clone.Name);
            Assert.Equal(self.Id, clone.Id);
        }

        [Fact]
        public void Export_Unknown_Key()
        {
            var password = "password".ToCharArray();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _keyStoreService.ExportAsync("unknown", password).Result;
            });
        }

        [Fact]
        public async Task Import_Wrong_Password()
        {
            const string name = "self";
            var _ = await _keyStoreService.CreateAsync(name, "ed25519", 0);

            var password = "password".ToCharArray();
            var pem = await _keyStoreService.ExportAsync("self", password);

            var wrong = "wrong password".ToCharArray();
            ExceptionAssert.Throws<UnauthorizedAccessException>(() =>
            {
                var _ = _keyStoreService.ImportAsync("clone", pem, wrong).Result;
            });
        }

        [Fact]
        public async Task Import_JSIPFS_Node()
        {
            const string pem = @"-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIFDTA/BgkqhkiG9w0BBQ0wMjAaBgkqhkiG9w0BBQwwDQQILdGJynKmkrMCAWQw
FAYIKoZIhvcNAwcECByaxdAET2tuBIIEyCKPITRayWR57HOJeTooJVR4tFCaNIo+
ThspwXbk+EqkhQUOcmn+OrgizxL9/sX1l+VlZYR9NkWqbaKo9yeZCX79p64MvUvp
IplgXxEf+rdfZ5xPQKN2Rfv7DyHW5h0JKMEISSLpgtA4Pc0Sr7PQdkLCS0tIF8yL
FEo7YA+yrmsUQbIeVabMLG0DaN2/csydp26IfldAOjqQgy5YIG/xz6gtISfXquxZ
YLPghpM4l/vK2IgxTbKPXQCLJ34rVRvIulIe0Zs+a9Om9O0uLZtTcM0VzlbIH6H7
fJlx6poxkBIG/Ui3nIjiOHa5DnqAxCNxkRH9TEBjoqFkPYQ1ExvfIIic2JqT8JUO
nKX9vGuudS/MqAEUO8EvrI68F4E+7zc/ahh/S3PQVhMZuR8ajblUZUxnItXgFt/0
mnOca3HNB2hLz83ubBvr6E9Nt/7AddIfaZkuIXkrXmz7LfelIsslUk4YIy7YchMv
Z1heLasChKVL7GEWoqXBv1erks+l8tpTe/iS/d4sWT8AiTFfPZu53TZ98vGtjLNO
RdaTNYP3tWyWCwQAPchHF9wLHCFjTrC2gsYgqalv2tYhHSqQg5lhDi9u+Z7bMihT
WzXVp7ddkYXX7wgD6yuPQWeKgkIlfKjiHMs6sfc5UBVEJWlgDtTl6DdUa0LaqGWF
J3b6Pc0f1NXNPN+iZlhO50eBunPUd0HT1tbdIwM9sqdDOe8+O55kUcXVeQAqPgj+
Qo0wi+L6bGSIhGXnoYPNn9al0ANNYV2Y2KJGPiWVEq8eLt3pFlhb3ELCEjgno9Sf
lYyIrP1hy9hfoN2sFM/s2Rn7/9n5u3/Gi9hVi8VlZGa7j4LLquP52ZLYBLOypwc3
2EDHw47r2TTKIcfiEwAEpuFuZDWyD0AgL0JwKYVKnwgzZIOiAW00Xn9FWmZQBtqK
Pb7jWArvmjvBK7cc0rLfLu2x4pF2DujeDxvru047nv97xiRpCpCLe6o5PRBotKkx
YYJQ7oS0u/n9gUbz45nR1DWXS3nAMCAXmNc6GLdFpjnTm8ivnnJi+8CBQvhUAKjK
BLKkOVFpTnDc8ha0ovtqCuKG2Ou+lU3z7BLqRCIGBbsHGxIC/aT708xGR9PhvARa
/y5NvdeDylnjX9pqa/zZXnO2844UfjQkXiJ5VP07MA0z4cQ56Isp1/UbT3+KCjRz
GT0HIE7AJhamgNE46ZHfhQfXSYhKxxPW3fd9wrFs55/1wTskfJgQFRYsrROZ6eDA
NyC8CV8reE/fgQk+0N/06lQ0/apqlEOC1uhNnS7b3AX7dk16BJNHsoMFs2SuDbOa
4skeCGK0oNjrbhYH9HIPQYoPEweE8QnebzSnxf9SRz0NlqS3vqsSEnIYY9F1t05u
YiNEZC4dk3vwRn6u3Br64fyg+dpYDWn+iSSWBan5Qof7uPht7QbZKCjLTRJcJVp2
lwUw2p8yaABLqSgEZ23jH5glGX3dJ1itAxiYxcFy/8GAbd+qTvaco73nRCS7ZeL+
FTrAh+xquPCw1yhbkeFtSVuUUqxQeXi9Zyq6kbeX+56HabAWPr3bg43zecFMM4tK
7xOA2p/9gala79mMYX49kYXwiP82nfouyyeSKv2jI9+6lejo+s4Lpnj6HfDsiJhl
Rw==
-----END ENCRYPTED PRIVATE KEY-----
";
            const string spki = "CAASpgIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfBYU9c0n28u02N/XCJY8yIsRqRVO5Zw+6kDHCremt2flHT4AaWnwGLAG9YyQJbRTvWN9nW2LK7Pv3uoIlvUSTnZEP0SXB5oZeqtxUdi6tuvcyqTIfsUSanLQucYITq8Qw3IMBzk+KpWNm98g9A/Xy30MkUS8mrBIO9pHmIZa55fvclDkTvLxjnGWA2avaBfJvHgMSTu0D2CQcmJrvwyKMhLCSIbQewZd2V7vc6gtxbRovKlrIwDTmDBXbfjbLljOuzg2yBLyYxXlozO9blpttbnOpU4kTspUVJXglmjsv7YSIJS3UKt3544l/srHbqlwC5CgOgjlwNfYPadO8kmBfAgMBAAE=";
            var password = "password".ToCharArray();

            await _keyStoreService.RemoveAsync("jsipfs");
            var key = await _keyStoreService.ImportAsync("jsipfs", pem, password);
            Assert.Equal("jsipfs", key.Name);
            Assert.Equal("QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb", key.Id);

            var pubKey = await _keyStoreService.GetPublicKeyAsync("jsipfs");
            Assert.Equal(spki, pubKey);
        }

        [Fact]
        public void Import_Bad_Format()
        {
            const string pem = @"this is not PEM";
            var password = "password".ToCharArray();
            ExceptionAssert.Throws<InvalidDataException>(() =>
            {
                var _ = _keyStoreService.ImportAsync("bad", pem, password).Result;
            });
        }

        [Fact]
        public void Import_Corrupted_Format()
        {
            const string pem = @"-----BEGIN ENCRYPTED PRIVATE KEY-----
MIIFDTA/BgkqhkiG9w0BBQ0wMjAaBgkqhkiG9w0BBQwwDQQILdGJynKmkrMCAWQw
-----END ENCRYPTED PRIVATE KEY-----
";
            var password = "password".ToCharArray();
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _keyStoreService.ImportAsync("bad", pem, password).Result;
            });
        }

        [Fact]
        public async Task Create_RSA_Key()
        {
            const string name = "net-engine-test-create";
            var key = await _keyStoreService.CreateAsync(name, "rsa", 512);
            try
            {
                Assert.NotNull(key);
                Assert.NotNull(key.Id);
                Assert.Equal(name, key.Name);

                var keys = await _keyStoreService.ListAsync();
                var clone = keys.Single(k => k.Name == name);
                Assert.Equal(key.Name, clone.Name);
                Assert.Equal(key.Id, clone.Id);
            }
            finally
            {
                await _keyStoreService.RemoveAsync(name);
            }
        }
        
        [Fact]
        public async Task Remove_Key()
        {
            const string name = "net-engine-test-remove";
            var key = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            var keys = await _keyStoreService.ListAsync();
            var clone = keys.Single(k => k.Name == name);
            Assert.NotNull(clone);

            var removed = await _keyStoreService.RemoveAsync(name);
            Assert.NotNull(removed);
            Assert.Equal(key.Name, removed.Name);
            Assert.Equal(key.Id, removed.Id);

            keys = await _keyStoreService.ListAsync();
            Assert.False(keys.Any(k => k.Name == name));
        }

        [Fact]
        public async Task Rename_Key()
        {
            const string name = "net-engine-test-rename0";
            const string newName = "net-engine-test-rename1";

            await _keyStoreService.RemoveAsync(name);
            await _keyStoreService.RemoveAsync(newName);
            var key = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            var renamed = await _keyStoreService.RenameAsync(name, newName);
            Assert.Equal(key.Id, renamed.Id);
            Assert.Equal(newName, renamed.Name);

            var keys = await _keyStoreService.ListAsync();
            var enumerable = keys as IKey[] ?? keys.ToArray();
            Assert.True(enumerable.Any(k => k.Name == newName));
            Assert.False(enumerable.Any(k => k.Name == name));
        }

        [Fact]
        public async Task Remove_Unknown_Key()
        {
            const string name = "net-engine-test-remove-unknown";

            var removed = await _keyStoreService.RemoveAsync(name);
            Assert.Null(removed);
        }

        [Fact]
        public async Task Rename_Unknown_Key()
        {
            const string name = "net-engine-test-rename-unknown";

            var renamed = await _keyStoreService.RenameAsync(name, "foobar");
            Assert.Null(renamed);
        }

        [Fact]
        public void Create_Unknown_KeyType()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = _keyStoreService.CreateAsync("unknown", "unknown", 0).Result;
            });
        }

        [Fact]
        public async Task UnsafeKeyName()
        {
            const string name = "../../../../../../../foo.key";
            var key = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            try
            {
                Assert.NotNull(key);
                Assert.NotNull(key.Id);
                Assert.Equal(name, key.Name);

                var keys = await _keyStoreService.ListAsync();
                var clone = keys.Single(k => k.Name == name);
                Assert.Equal(key.Name, clone.Name);
                Assert.Equal(key.Id, clone.Id);
            }
            finally
            {
                await _keyStoreService.RemoveAsync(name);
            }
        }

        [Fact]
        public async Task Create_Ed25519_Key()
        {
            const string name = "test-ed25519";
            var key = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            try
            {
                Assert.NotNull(key);
                Assert.NotNull(key.Id);
                Assert.Equal(name, key.Name);

                var keys = await _keyStoreService.ListAsync();
                var clone = keys.Single(k => k.Name == name);
                Assert.Equal(key.Name, clone.Name);
                Assert.Equal(key.Id, clone.Id);

                var priv = await _keyStoreService.GetPrivateKeyAsync(name);
                Assert.NotNull(priv);
                var pub = await _keyStoreService.GetPublicKeyAsync(name);
                Assert.NotNull(pub);

                // Verify key can be used as peer ID.
                var peer = new Peer
                {
                    Id = key.Id,
                    PublicKey = pub
                };
                Assert.True(peer.IsValid());
            }
            finally
            {
                await _keyStoreService.RemoveAsync(name);
            }
        }

        [Fact]
        public async Task Ed25519_Id_IdentityHash_of_PublicKey()
        {
            const string name = "test-ed25519-id-hash";
            var key = await _keyStoreService.CreateAsync(name, "ed25519", 0);
            Assert.Equal("identity", key.Id.Algorithm.Name);
        }

        [Fact]
        public async Task Import_OpenSSL_Ed25519()
        {
            // Created with:
            //   openssl genpkey -algorithm ED25519 -out k4.pem
            //   openssl  pkcs8 -nocrypt -in k4.pem -topk8 -out k4.nocrypt.pem
            const string pem = @"-----BEGIN PRIVATE KEY-----
MC4CAQAwBQYDK2VwBCIEIGJnyy3U4ksTQoRBz3mf1dxeFDPXZBrwh7gD7SqMg+/i
-----END PRIVATE KEY-----
";

            await _keyStoreService.RemoveAsync("oed1");
            var key = await _keyStoreService.ImportAsync("oed1", pem);
            Assert.Equal("oed1", key.Name);
            Assert.Equal("18n3naE9kBZoVvgYMV6saMZe3jn87dZiNbQ22BhxKTwU5yUoGfvBL1R3eScjokDGBk7i", key.Id);

            var privateKey = await _keyStoreService.GetPrivateKeyAsync("oed1");
            Assert.IsType(typeof(Ed25519PrivateKeyParameters), privateKey);
        }
    }
}
