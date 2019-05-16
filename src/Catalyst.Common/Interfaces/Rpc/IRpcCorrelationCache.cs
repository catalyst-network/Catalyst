using System;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.Rpc
{
    public interface IRpcCorrelationCache : IMessageCorrelationCache
    {
        TimeSpan CacheTtl { get; }
        void AddPendingRequest(PendingRequest pendingRequest);

        TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
            where TRequest : class, IMessage<TRequest>
            where TResponse : class, IMessage<TResponse>;
    }
}
