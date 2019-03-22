using System.Collections.Generic;

using Catalyst.Node.Common.Helpers.Shell;

namespace Catalyst.Node.Common.Interfaces 
{
    public interface IRpcNodesSettings
    {
        List<IRpcNodeConfig> NodesList { get; }
    }
}