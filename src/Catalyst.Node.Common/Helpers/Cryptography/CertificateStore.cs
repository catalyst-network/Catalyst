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
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Interfaces;
using Serilog;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.Cryptography;
using Catalyst.Node.Common.Interfaces.Filesystem;

namespace Catalyst.Node.Common.Helpers.Cryptography
{
    public sealed class CertificateStore : ICertificateStore
    {
        private const int PasswordReTries = 1;
        private const string LocalHost = "localhost";
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IPasswordReader _passwordReader;
        private readonly DirectoryInfo _storageFolder;
        private int PasswordTries { get; set; }

        public CertificateStore(IFileSystem fileSystem, IPasswordReader passwordReader)
        {
            _storageFolder = fileSystem.GetCatalystHomeDir();
            _passwordReader = passwordReader;
        }

        public X509Certificate2 ReadOrCreateCertificateFile(string pfxFilePath)
        {
            var foundCertificate = TryGet(pfxFilePath, out var certificate);

            if (foundCertificate)
            {
                return certificate;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                throw new PlatformNotSupportedException(
                    "Catalyst network currently doesn't support on the fly creation of self signed certificate. " +
                    $"Please create a password protected certificate at {pfxFilePath}." +
                    Environment.NewLine +
                    "cf. `https://github.com/catalyst-network/Catalyst.Node/wiki/Creating-a-Self-Signed-Certificate` for instructions");
            }

            certificate = CreateAndSaveSelfSignedCertificate(pfxFilePath);

            return certificate;
        }

        private X509Certificate2 CreateAndSaveSelfSignedCertificate(string filePath, string commonName = LocalHost)
        {
            var promptMessage = "Catalyst Node needs to create an SSL certificate." +
                " Please enter a password to encrypt the certificate on disk:";
            using (var password = _passwordReader.ReadSecurePassword(promptMessage))
            {
                var certificate = BuildSelfSignedServerCertificate(password, commonName);
                Save(certificate, filePath, password);
                return certificate;
            }
        }

        private void Save(X509Certificate2 certificate, string fileName, SecureString password)
        {
            var targetDirInfo = _storageFolder;
            if (!targetDirInfo.Exists)
            {
                targetDirInfo.Create();
            }

            var certificateInBytes = certificate.Export(X509ContentType.Pfx, password);
            var fullPathToCertificate = Path.Combine(targetDirInfo.FullName, fileName);
            File.WriteAllBytes(fullPathToCertificate, certificateInBytes);

            Logger.Warning("A certificate file has been created at {0}.",
                fullPathToCertificate);
            Logger.Warning("Please make sure this certificate is added to " +
                "your local trusted root store to remove warnings.", fullPathToCertificate);
        }

        private bool TryGet(string fileName, out X509Certificate2 certificate)
        {
            var fullPath = Path.Combine(_storageFolder.ToString(), fileName);
            var fileInfo = new FileInfo(fullPath);
            certificate = null;

            if (!fileInfo.Exists)
            {
                return false;
            }

            try
            {
                var fileInBytes = File.ReadAllBytes(fullPath);
                var passwordPromptMessage =
                    $"Please type in the password for the certificate at {fullPath} (optional):";
                var maxTries = 5;
                var tryCount = 0;
                while (tryCount <= maxTries)
                {
                    try
                    {
                        using (var passwordFromConsole = _passwordReader.ReadSecurePassword(passwordPromptMessage))
                        {
                            certificate = new X509Certificate2(fileInBytes, passwordFromConsole);
                            break;
                        }
                    }
                    catch (CryptographicException ex)
                    {
                        StringUtil.StringComparatorException(ex.Message.ToLowerInvariant(), "password");

                        PasswordAttemptCounter(ex.Message, fullPath);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to read certificate {0}", fullPath);
                return false;
            }

            return true;
        }

        private void PasswordAttemptCounter(string msg, string path)
        {
            PasswordTries++;
            if (PasswordTries == PasswordReTries)
            {
                Logger.Warning("The certificate at {0} requires a password to be read.", path);
            }
            else
            {
                Logger.Warning(msg);
            }
        }

        public static X509Certificate2 BuildSelfSignedServerCertificate(SecureString password,
            string commonName = LocalHost)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName(LocalHost);
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={commonName}");
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment |
                        X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection
                        {
                            new Oid("1.3.6.1.5.5.7.3.1") //server authentication
                        }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                    new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password,
                    X509KeyStorageFlags.Exportable);
            }
        }
    }
}
