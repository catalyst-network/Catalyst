using System.Net;

namespace Catalyst.Node.Common.Helpers.Shell {
    public interface IRpcNodeConfig {
        string NodeId { get; set; }
        IPAddress HostAddress { get; set; }
        int Port { get; set; }
        string PfxFileName { get; set; }
        string SslCertPassword { get; set; }
    }
}