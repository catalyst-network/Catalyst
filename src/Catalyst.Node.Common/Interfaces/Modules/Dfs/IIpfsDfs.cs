using System;
using Ipfs.CoreApi;
using PeerTalk;

namespace Catalyst.Node.Common.Interfaces.Modules.Dfs
{
    public interface IIpfsDfs : IDfs, ICoreApi, IService, IDisposable
    {
    }

    
}
