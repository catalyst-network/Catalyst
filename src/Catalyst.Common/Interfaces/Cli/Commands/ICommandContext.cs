using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;

namespace Catalyst.Common.Interfaces.Cli.Commands
{
    public interface ICommandContext
    {
        IPeerIdClientId PeerIdClientId { get; }
        IDtoFactory DtoFactory { get; }

        IPeerIdentifier PeerIdentifier { get; }

        INodeRpcClient GetConnectedNode(string nodeId);

        IRpcNodeConfig GetNodeConfig(string nodeId);

        bool IsSocketChannelActive(INodeRpcClient node);
    }
}
