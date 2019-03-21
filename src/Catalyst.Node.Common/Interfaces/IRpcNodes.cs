using System.Collections.Generic;

using Catalyst.Node.Common.Helpers.Shell;

namespace Catalyst.Node.Common.Interfaces 
{
    public interface IRpcNodes
    {
        List<RpcNode> nodesList { get; }
    }
}