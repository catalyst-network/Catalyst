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
using System.Security.Cryptography.X509Certificates;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Cryptography;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.UnitTests.Cryptography
{
    public sealed class CertificateStoreTests : FileSystemBasedTest
    {
        public CertificateStoreTests(ITestOutputHelper output) : base(output)
        {
            _passwordManager = Substitute.For<IPasswordManager>();
        }

        private string _fileWithPassName;
        private CertificateStore _certificateStore;
        private X509Certificate2 _createdCertificate;
        private X509Certificate2 _retrievedCertificate;

        private readonly IPasswordManager _passwordManager;

        private void Create_certificate_store()
        {
            _certificateStore = new CertificateStore(FileSystem, _passwordManager);
        }

        private void Ensure_no_certificate_file_exists()
        {
            var directoryInfo = FileSystem.GetCatalystDataDir();
            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }

            directoryInfo.Create();
            directoryInfo.EnumerateFiles().Should().BeEmpty();
        }

        private void Create_a_certificate_file_with_password()
        {
            _fileWithPassName = "test-with-pass.pfx";
            _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(Arg.Is(PasswordRegistryTypes.CertificatePassword)).Returns(TestPasswordReader.BuildSecureStringPassword("password"));
            _createdCertificate = _certificateStore.ReadOrCreateCertificateFile(_fileWithPassName);
        }

        private void The_store_should_have_asked_for_a_password_on_creation_and_loading()
        {
            _passwordManager.ReceivedWithAnyArgs(2).RetrieveOrPromptAndAddPasswordToRegistry(default);
        }

        private void Read_the_certificate_file_with_password()
        {
            _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(Arg.Is(PasswordRegistryTypes.CertificatePassword)).Returns(TestPasswordReader.BuildSecureStringPassword("password"));
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
