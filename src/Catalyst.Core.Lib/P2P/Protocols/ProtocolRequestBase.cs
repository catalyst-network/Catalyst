using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Util;
using Catalyst.Protocol.Peer;
using Dawn;
using Serilog;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    public abstract class ProtocolRequestBase : ProtocolBase
    {
        protected readonly ILogger Logger;
        protected readonly ICancellationTokenProvider CancellationTokenProvider;

        protected ProtocolRequestBase(ILogger logger,
            PeerId senderIdentifier,
            ICancellationTokenProvider cancellationTokenProvider,
            IPeerClient peerClient) : base(senderIdentifier)
        {
            Logger = logger;
            CancellationTokenProvider = cancellationTokenProvider;
            PeerClient = peerClient;
        }

        public IPeerClient PeerClient { get; }
        public bool Disposing { get; protected set; }
    }
}
