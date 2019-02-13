using System.Security.Cryptography.X509Certificates;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICertificateStore
    {
        IPasswordReader PasswordReader { get; }
        bool TryGet(string fileName, out X509Certificate2 certificate);
        X509Certificate2 CreateAndSaveSelfSignedCertificate(string filePath, string commonName = null);
    }
}