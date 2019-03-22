using System;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Core.UnitTest.TestUtils {
    public class ContextAnyObserver : IObserver<ContextAny>
    {
        private readonly ILogger _logger;

        public ContextAnyObserver(int index, ILogger logger)
        {
            _logger = logger;
            Index = index;
        }

        public ContextAny Received { get; private set; }
        public int Index { get; }

        public void OnCompleted() { _logger.Debug($"observer {Index} done"); }
        public void OnError(Exception error) { _logger.Debug($"observer {Index} received error : {error.Message}"); }

        public void OnNext(ContextAny value)
        {
            if (value == null) return;
            _logger.Debug($"observer {Index} received message of type {value?.Message.TypeUrl ?? "(null)"}");
            Received = value;
        }
    }
}