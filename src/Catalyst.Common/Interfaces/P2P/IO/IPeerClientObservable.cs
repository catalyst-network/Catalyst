using System;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;

namespace Catalyst.Common.Interfaces.P2P.IO
{
    public interface IPeerClientObservable
    {
        IObservable<IPeerClientMessageDto> MessageStream { get; }
    }
}
