using Ipfs.CoreApi;
using PeerTalk;

namespace Catalyst.Node.Common.Interfaces
{   
    public interface IIpfsConnector: ICoreApi
    {
        IService Service { get; }
    }
}
