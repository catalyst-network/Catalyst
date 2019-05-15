using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    /// <summary>
    /// The gossip message handler interface
    /// </summary>
    public interface IGossipMessageHandler
    {
        /// <summary>Starts the gossip.</summary>
        /// <param name="message">The message.</param>
        void StartGossip(IChanneledMessage<AnySigned> message);
    }
}
