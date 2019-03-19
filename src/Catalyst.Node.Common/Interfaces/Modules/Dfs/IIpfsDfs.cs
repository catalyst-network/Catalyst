using System;
using System.Threading;
using System.Threading.Tasks;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk;

namespace Catalyst.Node.Common.Interfaces.Modules.Dfs
{
    public interface IIpfsDfs : IDfs, ICoreApi, IService, IDisposable
    {
    }

    
}
