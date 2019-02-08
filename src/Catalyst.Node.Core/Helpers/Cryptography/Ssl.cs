using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Core.Helpers.Logger;
using Dawn;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public class Ssl
    {
        public static X509Certificate2 CreateCertificate(string password, string commonName)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={commonName}");

            using (var rsa = RSA.Create(2048)) //@TODO get the key size from configs
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment |
                        X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection {new Oid("1.3.6.1.5.5.7.3.1")}, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)),
                    new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                Log.Message("gen ssl");
                Log.Message(password);
                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password,
                    X509KeyStorageFlags.Exportable);
                //                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet);//@TODO this doesnt work on macosx https://github.com/dotnet/corefx/issues/19508
            }
        }

        public static X509Certificate2 LoadCert(string password, string dataDir, string fileName)
        {
            return LoadCert(password, $"{dataDir}/{fileName}");
        }

        public static X509Certificate2 LoadCert(string password, string filePath)
        {
            return string.IsNullOrEmpty(password)
                       ? new X509Certificate2(filePath)
                       : new X509Certificate2(filePath, password);
        }

        public static bool WriteCertificateFile(DirectoryInfo dataDir, string fileName, byte[] certificate)
        {
            var fullFilePath = Path.Combine(dataDir.FullName, fileName);
            File.WriteAllBytes(fullFilePath, certificate);
            return true;
        }

        public static bool Verify(byte[] data, X509Certificate2 publicKey, byte[] signature)
        {
            Guard.Argument(data, nameof(data)).NotNull();
            Guard.Argument(publicKey, nameof(publicKey)).NotNull();
            Guard.Argument(signature, nameof(signature)).NotNull();
            
            var provider = (RSACryptoServiceProvider) publicKey.PublicKey.Key;
            return provider.VerifyData(data, new SHA1CryptoServiceProvider(), signature);
        }

        public static byte[] Sign(byte[] data, X509Certificate2 privateKey)
        {
            Guard.Argument(data, nameof(data)).NotNull();
            Guard.Argument(privateKey, nameof(privateKey)).NotNull()
                 .Require(p => p.HasPrivateKey, p => "Invalid cerificate: Private key not found.");

            var provider = (RSACryptoServiceProvider) privateKey.PrivateKey;
            return provider.SignData(data, new SHA1CryptoServiceProvider());
        }

        public X509CertificateCollection GetCertificateCollection(X509Certificate2 certificate)
        {
            return new X509Certificate2Collection
               {
                   certificate
               };
        }
    }
}