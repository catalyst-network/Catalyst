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
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Types;
using Serilog;

namespace Catalyst.Core.Cryptography
{
    public sealed class CertificateStore
        : ICertificateStore
    {
        private readonly PasswordRegistryTypes _certificatePasswordIdentifier = PasswordRegistryTypes.CertificatePassword;
        private static int MaxTries => 5;
        private const string LocalHost = "localhost";
        private readonly DirectoryInfo _storageFolder;
        private readonly IPasswordManager _passwordManager;
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);
        private int PasswordTries { get; set; }

        public CertificateStore(IFileSystem fileSystem, IPasswordManager passwordManager)
        {
            _storageFolder = fileSystem.GetCatalystDataDir();
            _passwordManager = passwordManager;
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
            const string promptMessage = "Catalyst Node needs to create an SSL certificate." +
                " Please enter a password to encrypt the certificate on disk:";

            using (var password = _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(_certificatePasswordIdentifier, promptMessage))
            {
                var certificate = BuildSelfSignedServerCertificate(password, commonName);
                Save(certificate, filePath, password);
                return certificate;
            }
        }

        private void Save(X509Certificate certificate, string fileName, SecureString password)
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
                SecureString passwordFromConsole = null;
                while (PasswordTries < MaxTries)
                {
                    try
                    {
                        passwordFromConsole =
                            _passwordManager.RetrieveOrPromptAndAddPasswordToRegistry(_certificatePasswordIdentifier, passwordPromptMessage);
                        certificate = new X509Certificate2(fileInBytes, passwordFromConsole);

                        break;
                    }
                    catch (CryptographicException ex)
                    {
                        passwordFromConsole?.Dispose();
                        PasswordTries++;

                        if (PasswordTries >= MaxTries)
                        {
                            throw new InvalidCredentialException(
                                $"Failed to obtain the correct password for certificate {fullPath} from the console after {MaxTries} attempts.");
                        }

                        Logger.Warning(ex.Message);
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
