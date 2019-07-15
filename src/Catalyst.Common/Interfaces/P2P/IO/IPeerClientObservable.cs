using System;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;

namespace Catalyst.Common.Interfaces.P2P.IO
{
    public interface IPeerClientObservable
    {
        ReplaySubject<IPeerClientMessageDto> _responseMessageSubject { get; }
        IObservable<IPeerClientMessageDto> MessageStream { get; }
    }
}
