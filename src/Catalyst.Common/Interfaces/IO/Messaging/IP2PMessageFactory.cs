using System;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    /// <summary>
    /// The P2P message factory
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IP2PMessageFactory<TMessage> where TMessage : class, IMessage<TMessage>
    {
        /// <summary>Gets the message in datagram envelope.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns></returns>
        IByteBufferHolder GetMessageInDatagramEnvelope(TMessage message, 
            IPeerIdentifier recipient,
            IPeerIdentifier sender, 
            MessageTypes messageType, 
            Guid correlationId = default);
    }
}
