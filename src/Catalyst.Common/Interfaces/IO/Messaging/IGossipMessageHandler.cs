using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IGossipMessageHandler
    {
        void StartGossip(IChanneledMessage<AnySigned> message);
    }
}
