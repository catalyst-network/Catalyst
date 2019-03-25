using System;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Core.UnitTest.TestUtils {
    public class AnyMessageObserver : IObserver<IChanneledMessage<Any>>
    {
        private readonly ILogger _logger;

        public AnyMessageObserver(int index, ILogger logger)
        {
            _logger = logger;
            Index = index;
        }

        public IChanneledMessage<Any> Received { get; private set; }
        public int Index { get; }

        public void OnCompleted() { _logger.Debug($"observer {Index} done"); }
        public void OnError(Exception error) { _logger.Debug($"observer {Index} received error : {error.Message}"); }

        public void OnNext(IChanneledMessage<Any> value)
        {
            if (value == NullObjects.ChanneledAny) return;
            _logger.Debug($"observer {Index} received message of type {value?.Payload.TypeUrl ?? "(null)"}");
            Received = value;
        }
    }
}