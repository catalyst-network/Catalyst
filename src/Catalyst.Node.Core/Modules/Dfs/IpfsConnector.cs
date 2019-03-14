using Catalyst.Node.Common.Interfaces;
using Ipfs.CoreApi;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsConnector : IIpfsConnector
    {
        public ICoreApi CoreApi { get; }

        public IpfsConnector(ICoreApi coreApi) { CoreApi = coreApi; }
    }
}