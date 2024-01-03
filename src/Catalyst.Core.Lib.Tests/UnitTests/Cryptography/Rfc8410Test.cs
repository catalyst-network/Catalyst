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

using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using Makaretu.Dns;
using MultiFormats;
using NSubstitute;
using Org.BouncyCastle.Crypto.Parameters;
using NUnit.Framework;


namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class Rfc8410Test : FileSystemBasedTest
    {
        private KeyStoreService _keyStoreService;
        
        [SetUp]
        public void Init()
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
        public async Task ReadPrivateKey()
        {
            const string alice1 = @"-----BEGIN PRIVATE KEY-----
MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
-----END PRIVATE KEY-----
";
            var _ = await _keyStoreService.ImportAsync("alice1", alice1);
            try
            {
                var priv = (Ed25519PrivateKeyParameters) await _keyStoreService.GetPrivateKeyAsync("alice1");
                Assert.Equals(priv.IsPrivate, true);
                Assert.Equals("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842",
                    priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.Equals(pub.IsPrivate, false);
                Assert.Equals("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1",
                    pub.GetEncoded().ToHexString());
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice1");
            }
        }

        [Test]
        public async Task ReadPrivateAndPublicKey()
        {
            const string alice1 = @"-----BEGIN PRIVATE KEY-----
MHICAQEwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
oB8wHQYKKoZIhvcNAQkJFDEPDA1DdXJkbGUgQ2hhaXJzgSEAGb9ECWmEzf6FQbrB
Z9w7lshQhqowtrbLDFw4rXAxZuE=
-----END PRIVATE KEY-----
";
            var _ = await _keyStoreService.ImportAsync("alice1", alice1);
            try
            {
                var priv = (Ed25519PrivateKeyParameters) await _keyStoreService.GetPrivateKeyAsync("alice1");
                Assert.Equals(priv.IsPrivate, true);
                Assert.Equals("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842",
                    priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.Equals(pub.IsPrivate, false);
                Assert.Equals("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1",
                    pub.GetEncoded().ToHexString());
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice1");
            }
        }
    }
}
