using System;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.IO.Observers
{
    public interface IPeerClientObservable
    {
        IObservable<IPeerClientMessageDto> MessageStream { get; }
    }
}
