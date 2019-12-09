using System.Security;
using System.Threading.Tasks;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using MultiFormats;
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
            _keyStoreService = new KeyStoreService(FileSystem.Path.ToString())
            {
                Options = new DfsOptions().KeyChain
            };
            var securePassword = new SecureString();

            foreach (char c in "mypassword")
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            _keyStoreService.SetPassphraseAsync(securePassword).ConfigureAwait(false);
        }

        [Fact]
        public async Task Create_Rsa()
        {
            var key = await _keyStoreService.CreateAsync("alice", "rsa", 512);
            try
            {
                var cert = await _keyStoreService.CreateBCCertificateAsync(key.Name);
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
                var cert = await _keyStoreService.CreateBCCertificateAsync("alice");
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
                var cert = await _keyStoreService.CreateBCCertificateAsync("alice");
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
