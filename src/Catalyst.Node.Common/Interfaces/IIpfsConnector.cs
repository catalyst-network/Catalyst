using Ipfs.CoreApi;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IIpfsConnector
    {
        ICoreApi CoreApi { get; }
    }
}