using System.Net;

namespace Catalyst.Node.Modules.Core.Mempool
{
    public interface IMempoolSettings
    {
        IPEndPoint Host { get; set; }
        string Type { get; set; }
        string When { get; set; }
    }
}