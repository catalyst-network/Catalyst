using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    public interface ICertificateStore
    {
        IPasswordReader PasswordReader { get; }
        X509Certificate2 GetCertificateFromFile(string fileName);
        X509Certificate2 CreateAndSaveSelfSignedCertificate(string filePath, string commonName = null);
    }
}