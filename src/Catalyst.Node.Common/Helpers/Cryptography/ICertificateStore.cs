using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    public interface ICertificateStore
    {
        X509Certificate2 ReadOrCreateCertificateFile(string fileName);
    }
}