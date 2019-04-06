using System;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class PeerClient : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ICertificateStore _certificateStore;
        private readonly AnyTypeClientHandler _clientHandler;
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        private readonly PingRequestHandler _pingRequestHandler;

        public PeerClient(ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _certificateStore = certificateStore;
            _clientHandler = new AnyTypeClientHandler();
            MessageStream = _clientHandler.MessageStream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("disposing peerClient");
                _pingRequestHandler.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
