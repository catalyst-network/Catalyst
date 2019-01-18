using System.Net;

namespace Catalyst.Node.Modules.Core.Rpc
{
    public interface IRpcSettings
    {
        int Port { get; set; }
        IPAddress BindAddress { get; set; }
    }
}