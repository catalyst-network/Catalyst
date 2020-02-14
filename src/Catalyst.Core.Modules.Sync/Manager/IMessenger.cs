using Catalyst.Protocol.Peer;
using Google.Protobuf;
using System.Collections.Generic;

namespace Catalyst.Core.Modules.Sync.Manager
{
    public interface IMessenger
    {
        void SendMessageToPeers(IMessage message, IEnumerable<PeerId> peers);
    }
}
