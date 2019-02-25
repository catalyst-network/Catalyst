using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    public interface ICertificateStore
    {
        IPasswordReader PasswordReader { get; }
        bool TryGet(string fileName, out X509Certificate2 certificate);
        X509Certificate2 CreateAndSaveSelfSignedCertificate(string filePath, string commonName = null);
    }
}