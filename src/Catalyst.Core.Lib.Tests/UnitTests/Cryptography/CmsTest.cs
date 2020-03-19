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
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using Makaretu.Dns;
using NSubstitute;
using NUnit.Framework;


namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class CmsTest : FileSystemBasedTest
    {
        private readonly KeyStoreService _keyStoreService;

        public CmsTest(TestContext output) : base(output)
        {
            var dfsOptions = new DfsOptions(new BlockOptions(), new DiscoveryOptions(), new RepositoryOptions(FileSystem, Constants.DfsDataSubDir), Substitute.For<KeyChainOptions>(), Substitute.For<SwarmOptions>(), Substitute.For<IDnsClient>());
            _keyStoreService = new KeyStoreService(dfsOptions)
            {
                Options = dfsOptions.KeyChain
            };
            var securePassword = new SecureString();

            "mypassword".ToList().ForEach(c => securePassword.AppendChar(c));

            securePassword.MakeReadOnly();
            _keyStoreService.SetPassphraseAsync(securePassword).ConfigureAwait(false);
        }
        
        [Test]
        public async Task ReadCms()
        {
            string aliceKid = "QmNzBqPwp42HZJccsLtc4ok6LjZAspckgs2du5tTmjPfFA";
            string alice = @"-----BEGIN ENCRYPTED PRIVATE KEY-----
MIICxjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQIMhYqiVoLJMICAggA
MBQGCCqGSIb3DQMHBAhU7J9bcJPLDQSCAoDzi0dP6z97wJBs3jK2hDvZYdoScknG
QMPOnpG1LO3IZ7nFha1dta5liWX+xRFV04nmVYkkNTJAPS0xjJOG9B5Hm7wm8uTd
1rOaYKOW5S9+1sD03N+fAx9DDFtB7OyvSdw9ty6BtHAqlFk3+/APASJS12ak2pg7
/Ei6hChSYYRS9WWGw4lmSitOBxTmrPY1HmODXkR3txR17LjikrMTd6wyky9l/u7A
CgkMnj1kn49McOBJ4gO14c9524lw9OkPatyZK39evFhx8AET73LrzCnsf74HW9Ri
dKq0FiKLVm2wAXBZqdd5ll/TPj3wmFqhhLSj/txCAGg+079gq2XPYxxYC61JNekA
ATKev5zh8x1Mf1maarKN72sD28kS/J+aVFoARIOTxbG3g+1UbYs/00iFcuIaM4IY
zB1kQUFe13iWBsJ9nfvN7TJNSVnh8NqHNbSg0SdzKlpZHHSWwOUrsKmxmw/XRVy/
ufvN0hZQ3BuK5MZLixMWAyKc9zbZSOB7E7VNaK5Fmm85FRz0L1qRjHvoGcEIhrOt
0sjbsRvjs33J8fia0FF9nVfOXvt/67IGBKxIMF9eE91pY5wJNwmXcBk8jghTZs83
GNmMB+cGH1XFX4cT4kUGzvqTF2zt7IP+P2cQTS1+imKm7r8GJ7ClEZ9COWWdZIcH
igg5jozKCW82JsuWSiW9tu0F/6DuvYiZwHS3OLiJP0CuLfbOaRw8Jia1RTvXEH7m
3N0/kZ8hJIK4M/t/UAlALjeNtFxYrFgsPgLxxcq7al1ruG7zBq8L/G3RnkSjtHqE
cn4oisOvxCprs4aM9UVjtZTCjfyNpX8UWwT1W3rySV+KQNhxuMy3RzmL
-----END ENCRYPTED PRIVATE KEY-----
";
            var key = await _keyStoreService.ImportAsync("alice", alice, "mypassword".ToArray());
            try
            {
                Assert.AreEqual(aliceKid, key.Id);

                var cipher = Convert.FromBase64String(@"
MIIBcwYJKoZIhvcNAQcDoIIBZDCCAWACAQAxgfowgfcCAQAwYDBbMQ0wCwYDVQQK
EwRpcGZzMREwDwYDVQQLEwhrZXlzdG9yZTE3MDUGA1UEAxMuUW1OekJxUHdwNDJI
WkpjY3NMdGM0b2s2TGpaQXNwY2tnczJkdTV0VG1qUGZGQQIBATANBgkqhkiG9w0B
AQEFAASBgLKXCZQYmMLuQ8m0Ex/rr3KNK+Q2+QG1zIbIQ9MFPUNQ7AOgGOHyL40k
d1gr188EHuiwd90PafZoQF9VRSX9YtwGNqAE8+LD8VaITxCFbLGRTjAqeOUHR8cO
knU1yykWGkdlbclCuu0NaAfmb8o0OX50CbEKZB7xmsv8tnqn0H0jMF4GCSqGSIb3
DQEHATAdBglghkgBZQMEASoEEP/PW1JWehQx6/dsLkp/Mf+gMgQwFM9liLTqC56B
nHILFmhac/+a/StQOKuf9dx5qXeGvt9LnwKuGGSfNX4g+dTkoa6N
");
                var plain = await _keyStoreService.ReadProtectedDataAsync(cipher);
                var plainText = Encoding.UTF8.GetString(plain);
                Assert.AreEqual("This is a message from Alice to Bob", plainText);
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }

        [Test]
        public void ReadCms_FailsWithoutKey()
        {
            var cipher = Convert.FromBase64String(@"
MIIBcwYJKoZIhvcNAQcDoIIBZDCCAWACAQAxgfowgfcCAQAwYDBbMQ0wCwYDVQQK
EwRpcGZzMREwDwYDVQQLEwhrZXlzdG9yZTE3MDUGA1UEAxMuUW1OekJxUHdwNDJI
WkpjY3NMdGM0b2s2TGpaQXNwY2tnczJkdTV0VG1qUGZGQQIBATANBgkqhkiG9w0B
AQEFAASBgLKXCZQYmMLuQ8m0Ex/rr3KNK+Q2+QG1zIbIQ9MFPUNQ7AOgGOHyL40k
d1gr188EHuiwd90PafZoQF9VRSX9YtwGNqAE8+LD8VaITxCFbLGRTjAqeOUHR8cO
knU1yykWGkdlbclCuu0NaAfmb8o0OX50CbEKZB7xmsv8tnqn0H0jMF4GCSqGSIb3
DQEHATAdBglghkgBZQMEASoEEP/PW1JWehQx6/dsLkp/Mf+gMgQwFM9liLTqC56B
nHILFmhac/+a/StQOKuf9dx5qXeGvt9LnwKuGGSfNX4g+dTkoa6N
");
            ExceptionAssert.Throws<KeyNotFoundException>(() =>
            {
                var _ = _keyStoreService.ReadProtectedDataAsync(cipher).Result;
            });
        }

        [Test]
        public async Task CreateCms_Rsa()
        {
            var _ = await _keyStoreService.CreateAsync("alice", "rsa", 512);
            try
            {
                var data = new byte[] {1, 2, 3, 4};
                var cipher = await _keyStoreService.CreateProtectedDataAsync("alice", data);
                var plain = await _keyStoreService.ReadProtectedDataAsync(cipher);
                Assert.AreEqual(data, plain);
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }

        [Test]
        public async Task CreateCms_Secp256k1()
        {
            var _ = await _keyStoreService.CreateAsync("alice", "secp256k1", 0);
            try
            {
                var data = new byte[] {1, 2, 3, 4};
                var cipher = await _keyStoreService.CreateProtectedDataAsync("alice", data);
                var plain = await _keyStoreService.ReadProtectedDataAsync(cipher);
                Assert.AreEqual(data, plain);
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }

        // [Test]
        // [Skip("NYI")]
        // public async Task CreateCms_Ed25519()
        // {
        //     var ipfs = TestFixture.Ipfs;
        //     var keychain = await ipfs.KeyChainAsync();
        //     var key = await ipfs.Key.CreateAsync("alice", "ed25519", 0);
        //     try
        //     {
        //         var data = new byte[] {1, 2, 3, 4};
        //         var cipher = await keychain.CreateProtectedDataAsync("alice", data);
        //         var plain = await keychain.ReadProtectedDataAsync(cipher);
        //         CollectionAssert.AreEqual(data, plain);
        //     }
        //     finally
        //     {
        //         await ipfs.Key.RemoveAsync("alice");
        //     }
        // }
    }
}
