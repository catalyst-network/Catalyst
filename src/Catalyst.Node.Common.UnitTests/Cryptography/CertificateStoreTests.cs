using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.UnitTests.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Common.UnitTests.Cryptography
{
    public class CertificateStoreTests : FileSystemBasedTest, IDisposable
    {
        public CertificateStoreTests(ITestOutputHelper output) : base(output)
        {
            _passwordReader = Substitute.For<IPasswordReader>();
        }

        private string _fileWithPassName;
        private DirectoryInfo _directoryInfo;
        private CertificateStore _certificateStore;
        private X509Certificate2 _createdCertificate;
        private X509Certificate2 _retrievedCertificate;

        private readonly IPasswordReader _passwordReader;

        private SecureString BuildSecureStringPassword()
        {
            var secureString = new SecureString();
            "password".ToList().ForEach(c => secureString.AppendChar(c));
            secureString.MakeReadOnly();
            return secureString;
        }

        private void Create_certificate_store()
        {
            var dataFolder = _fileSystem.GetCatalystHomeDir().FullName;
            _directoryInfo = new DirectoryInfo(dataFolder);
            _certificateStore = new CertificateStore(_fileSystem, _passwordReader);
        }

        private void Ensure_no_certificate_file_exists()
        {
            _directoryInfo = _fileSystem.GetCatalystHomeDir();
            if (_directoryInfo.Exists)
            {
                _directoryInfo.Delete(true);
            }
            _directoryInfo.Create();
            _directoryInfo.EnumerateFiles().Should().BeEmpty();
        }

        private void Create_a_certificate_file_with_password()
        {
            _fileWithPassName = "test-with-pass.pfx";
            _passwordReader.ReadSecurePassword().Returns(BuildSecureStringPassword());
            _createdCertificate = _certificateStore.ReadOrCreateCertificateFile(_fileWithPassName);
        }

        private void The_store_should_have_asked_for_a_password_on_creation_and_loading()
        {
            _passwordReader.ReceivedWithAnyArgs(2).ReadSecurePassword(null);
        }

        private void Read_the_certificate_file_with_password()
        {
            _passwordReader.ReadSecurePassword().Returns(BuildSecureStringPassword());
            _retrievedCertificate = _certificateStore.ReadOrCreateCertificateFile(_fileWithPassName);
        }

        private void The_certificate_from_file_should_have_the_correct_thumbprint()
        {
            _retrievedCertificate.Should().NotBeNull();
            _retrievedCertificate.Thumbprint.Should().Be(_createdCertificate.Thumbprint);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _createdCertificate?.Dispose();
            _retrievedCertificate?.Dispose();
        }

        [Fact]
        public void CertificateStore_CanReadAndWriteCertFiles_WithPassword()
        {
            //TODO: cf. issue <see cref="https://github.com/catalyst-network/Catalyst.Node/issues/2" />
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return;
            }

            Create_certificate_store();
            Ensure_no_certificate_file_exists();
            Create_a_certificate_file_with_password();
            Read_the_certificate_file_with_password();
            The_store_should_have_asked_for_a_password_on_creation_and_loading();
            The_certificate_from_file_should_have_the_correct_thumbprint();
        }
    }
}