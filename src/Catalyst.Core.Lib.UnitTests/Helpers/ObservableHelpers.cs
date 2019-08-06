using System;
using System.Reactive.Linq;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using NSubstitute;

namespace Catalyst.Core.Lib.UnitTests.Helpers
{
    public static class ObservableHelpers
    {
        public static IObservableChannel MockObservableChannel(IObservable<IObserverDto<ProtocolMessage>> replaySubject)
        {
            var mockChannel = Substitute.For<IChannel>();
            var mockEventStream = replaySubject.AsObservable();
            var observableChannel = new ObservableChannel(mockEventStream, mockChannel);
            return observableChannel;
        }
    }
}
