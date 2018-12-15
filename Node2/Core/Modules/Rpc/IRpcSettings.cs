using Microsoft.Extensions.Configuration;

namespace ADL.Node.Core.Modules.Rpc
{
    public interface IRpcSettings
    {
        int Port { get; set; }
        string BindAddress { get; set; }
    }
}
