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
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509.Extension;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class CertTest : FileSystemBasedTest
    {
        private readonly KeyStoreService _keyStoreService;

        public CertTest(ITestOutputHelper output) : base(output)
        {
            var dfsOptions = new DfsOptions(Substitute.For<BlockOptions>(), Substitute.For<DiscoveryOptions>(), new RepositoryOptions(FileSystem, Constants.DfsDataSubDir), Substitute.For<KeyChainOptions>(), Substitute.For<SwarmOptions>(), Substitute.For<DotClient>());
            _keyStoreService = new KeyStoreService(dfsOptions)
            {
                Options = dfsOptions.KeyChain
            };
            var securePassword = new SecureString();

            "mypassword".ToList().ForEach(c => securePassword.AppendChar(c));

            securePassword.MakeReadOnly();
            _keyStoreService.SetPassphraseAsync(securePassword).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_Rsa()
        {
            var key = await _keyStoreService.CreateAsync("alice", "rsa", 512);
            try
            {
                var cert = await _keyStoreService.CreateBcCertificateAsync(key.Name);
                Assert.Equal($"CN={key.Id},OU=keystore,O=ipfs", cert.SubjectDN.ToString());
                var ski = new SubjectKeyIdentifierStructure(
                    cert.GetExtensionValue(X509Extensions.SubjectKeyIdentifier));
                Assert.Equal(key.Id.ToBase58(), ski.GetKeyIdentifier().ToBase58());
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }

        [Fact]
        public async Task Create_Secp256k1()
        {
            var key = await _keyStoreService.CreateAsync("alice", "secp256k1", 0);
            try
            {
                var cert = await _keyStoreService.CreateBcCertificateAsync("alice");
                Assert.Equal($"CN={key.Id},OU=keystore,O=ipfs", cert.SubjectDN.ToString());
                var ski = new SubjectKeyIdentifierStructure(
                    cert.GetExtensionValue(X509Extensions.SubjectKeyIdentifier));
                Assert.Equal(key.Id.ToBase58(), ski.GetKeyIdentifier().ToBase58());
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }

        [Fact]
        public async Task Create_Ed25519()
        {
            var key = await _keyStoreService.CreateAsync("alice", "ed25519", 0);
            try
            {
                var cert = await _keyStoreService.CreateBcCertificateAsync("alice");
                Assert.Equal($"CN={key.Id},OU=keystore,O=ipfs", cert.SubjectDN.ToString());
                var ski = new SubjectKeyIdentifierStructure(
                    cert.GetExtensionValue(X509Extensions.SubjectKeyIdentifier));
                Assert.Equal(key.Id.ToBase58(), ski.GetKeyIdentifier().ToBase58());
            }
            finally
            {
                await _keyStoreService.RemoveAsync("alice");
            }
        }
    }
}
