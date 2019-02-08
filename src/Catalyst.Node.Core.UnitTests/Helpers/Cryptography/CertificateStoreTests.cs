using System.IO;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Core.Helpers.Cryptography;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.Helpers.Cryptography
{
    public class CertificateStoreTests
    {
        public class CertificateStoreSpecifications
        {
            private string fileWithPassName;
            private string fileWithoutPassName;
            private DirectoryInfo directoryInfo;
            private CertificateStore certificateStore;
            private X509Certificate2 createdCertificate;
            private X509Certificate2 retrievedCertificate;

            private IPasswordReader passwordReader;

            public CertificateStoreSpecifications(ITestOutputHelper output) { }

            [Fact]
            public void CertificateStoreCanReadAndWriteCertFiles_WithPassword_ByAskingPassword() { }

            [Fact]
            public void CertificateStoreCanReadAndWriteCertFiles_WithoutPassword_WithouthAskingPassword() { }
        }
    }
}