using System.Net;

namespace Catalyst.Node.Common.Helpers.Shell
{
    public class RpcNode
    {
        public string NodeId { get; set; }
        public IPAddress HostAddress { get; set; }
        public int Port { get; set; }
        public string PfxFileName { get; set; }
        public string SslCertPassword { get; set; }
    }
}