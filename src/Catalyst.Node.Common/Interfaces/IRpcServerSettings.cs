using System.Net;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IRpcServerSettings
    {
        int Port { get; }
        IPAddress BindAddress { get; }
        bool MutualAuthentication { get; }
        bool AcceptInvalidCerts { get; }
        string PfxFileName { get; set; }
    }
}