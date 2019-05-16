using System;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IMessageFactoryBase
    {
        /// <summary>Gets the message.</summary>
        /// <param name="messageDto">The message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>AnySigned message</returns>
        AnySigned GetMessage(IMessageDto messageDto,
            Guid correlationId = default);
    }
}
