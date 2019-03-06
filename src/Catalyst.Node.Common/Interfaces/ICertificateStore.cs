using System.Security.Cryptography.X509Certificates;

namespace Catalyst.Node.Common.Interfaces
{
    public interface ICertificateStore
    {
        X509Certificate2 ReadOrCreateCertificateFile(string fileName);
    }
}