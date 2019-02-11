using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.Helpers.Cryptography;
using Xunit;
using Xunit.Abstractions;
using NSubstitute;
using FluentAssertions;
using Serilog;

namespace Catalyst.Node.Core.UnitTest.Helpers.Cryptography
{
    public class CertificateStoreTests
    {
        private string _fileWithPassName;
        private string _fileWithoutPassName;
        private DirectoryInfo _directoryInfo;
        private CertificateStore _certificateStore;
        private X509Certificate2 _createdCertificate;
        private X509Certificate2 _retrievedCertificate;

        private readonly IPasswordReader _passwordReader;
        private readonly ILogger _logger;

        /// <summary>
        /// This can for instance be used to find the name of the test currently using the
        /// class by calling this.CurrentTest.DisplayName
        /// </summary>
        private readonly ITest _currentTest;
        private readonly string _currentTestName;
        private readonly ITestOutputHelper _output;
        private readonly IFileSystem _fileSystem;

        public CertificateStoreTests(ITestOutputHelper output)
        {
            _output = output;
            _currentTest = (_output?.GetType()
                                   .GetField("test", BindingFlags.Instance | BindingFlags.NonPublic)
                                   .GetValue(_output) as ITest);
            _currentTestName = _currentTest.TestCase.TestMethod.Method.Name;
            var testDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, _currentTestName));

            _fileSystem = Substitute.For<IFileSystem>();
            _fileSystem.GetCatalystHomeDir().Returns(testDirectory);

            _logger = Substitute.For<ILogger>();
            _passwordReader = Substitute.For<IPasswordReader>();
        }

        [Fact]
        public void CertificateStoreCanReadAndWriteCertFiles_WithPassword_ByAskingPassword()
        {
            Create_certificate_store();
            Ensure_no_certificate_file_exists();
            Create_a_certificate_file_with_password();
            
            Read_the_certificate_file_with_password();
            
            The_store_should_ask_for_a_password();
            The_certificate_from_file_should_have_the_correct_thumbprint();
        }

        [Fact]
        public void CertificateStoreCanReadAndWriteCertFiles_WithoutPassword_WithoutAskingPassword()
        {
            Create_certificate_store();
            Ensure_no_certificate_file_exists();
            Create_a_certificate_file_without_password();
            Read_the_certificate_file_without_password();
            The_store_should_not_ask_for_a_password();
            The_certificate_from_file_should_have_the_correct_thumbprint();
        }

        private void Create_certificate_store()
        {
            var dataFolder = Path.Combine(Environment.CurrentDirectory, _currentTestName);
            _directoryInfo = new DirectoryInfo(dataFolder);

            _passwordReader.ReadSecurePassword(Arg.Any<string>()).Returns(BuildSecureStringPassword());

            _certificateStore = new CertificateStore(_fileSystem, _passwordReader, _logger);
        }

        private void Ensure_no_certificate_file_exists()
        {
            var dataFolder = Path.Combine(Environment.CurrentDirectory, _currentTestName);
            _directoryInfo = new DirectoryInfo(dataFolder);
            if(!_directoryInfo.Exists) _directoryInfo.Create();
            _directoryInfo.EnumerateFiles().Should().BeEmpty();

            _fileWithPassName = "test-with-pass.pfx";
            _fileWithoutPassName = "test-without-pass.pfx";
            _certificateStore.TryGet(_fileWithPassName, out var _).Should().BeFalse();
            _certificateStore.TryGet(_fileWithoutPassName, out var _).Should().BeFalse();
        }

        private SecureString BuildSecureStringPassword()
        {
            var secureString = new SecureString();
            "password".ToList().ForEach(c => secureString.AppendChar(c));
            secureString.MakeReadOnly();
            return secureString;
        }

        private void Create_a_certificate_file_with_password()
        {
            using (var password = BuildSecureStringPassword())
            {
                _createdCertificate = _certificateStore.BuildSelfSignedServerCertificate(password);
                _certificateStore.Save(_createdCertificate, _fileWithPassName, password);
            }
        }

        private void Create_a_certificate_file_without_password()
        {
            _createdCertificate = _certificateStore.BuildSelfSignedServerCertificate(new SecureString());
            _certificateStore.Save(_createdCertificate, _fileWithoutPassName, new SecureString());
        }

        private void The_store_should_ask_for_a_password()
        {
            _passwordReader.ReceivedWithAnyArgs(1).ReadSecurePassword(null);
        }

        private void The_store_should_not_ask_for_a_password()
        {
            _passwordReader.DidNotReceiveWithAnyArgs().ReadSecurePassword(null);
        }

        private void Read_the_certificate_file_without_password()
        {
            _retrievedCertificate = null;
            _certificateStore.TryGet(_fileWithoutPassName, out _retrievedCertificate).Should().BeTrue();
        }

        private void Read_the_certificate_file_with_password()
        {
            _retrievedCertificate = null;
            _certificateStore.TryGet(_fileWithPassName, out _retrievedCertificate).Should().BeTrue();
        }

        private void The_certificate_from_file_should_have_the_correct_thumbprint()
        {
            _retrievedCertificate.Should().NotBeNull();
            _retrievedCertificate.Thumbprint.Should().Be(_createdCertificate.Thumbprint);
        }
    }
}