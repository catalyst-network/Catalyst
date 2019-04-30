using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// Peer count request handler
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetPeerCountRequest, CatalystIMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public class PeerCountRequestHandler
        : CorrelatableMessageHandlerBase<GetPeerCountRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The RPC message base</summary>
        private readonly RpcMessageFactory<GetPeerCountResponse, RpcMessages> _rpcMessageBase;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="PeerCountRequestHandler"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="peerDiscovery">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public PeerCountRequestHandler(IPeerIdentifier peerIdentifier, IMessageCorrelationCache correlationCache, IPeerDiscovery peerDiscovery, ILogger logger) :
            base(correlationCache, logger)
        {
            _peerDiscovery = peerDiscovery;
            _rpcMessageBase = new RpcMessageFactory<GetPeerCountResponse, RpcMessages>();
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var peerCount = _peerDiscovery.PeerRepository.GetAll().Count();

            var response = new GetPeerCountResponse
            {
                PeerCount = peerCount
            };

            var responseMessage = _rpcMessageBase.GetMessage(new MessageDto<GetPeerCountResponse, RpcMessages>(
                type: RpcMessages.PeerListCountResponse,
                message: response,
                recipient: new PeerIdentifier(message.Payload.PeerId),
                sender: _peerIdentifier
            ));

            message.Context.Channel.WriteAndFlushAsync(responseMessage);
        }
    }
}
