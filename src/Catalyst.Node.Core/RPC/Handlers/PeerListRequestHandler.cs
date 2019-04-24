#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;
using System.Collections.Generic;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// Handles the PeerListRequest message
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetPeerListRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class PeerListRequestHandler
        : CorrelatableMessageHandlerBase<GetPeerListRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>
        /// The peer identifier
        /// </summary>
        private readonly PeerId _peerId;

        /// <summary>
        /// The peer list
        /// </summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>
        /// The message
        /// </summary>
        private IChanneledMessage<AnySigned> _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestHandler"/> class.
        /// </summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="messageCorrelationCache">The message correlation cache.</param>
        /// <param name="peerDiscovery">The peer list</param>
        public PeerListRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IMessageCorrelationCache messageCorrelationCache,
            IPeerDiscovery peerDiscovery)
            : base(messageCorrelationCache, logger)
        {
            _peerId = peerIdentifier.PeerId;
            _peerDiscovery = peerDiscovery;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            _message = message;

            ReturnResponse(this._peerDiscovery.PeerRepository.GetAll().Select(x => x.PeerIdentifier.PeerId));

            Logger.Debug("received message of type PeerListRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="peers">The peers list</param>
        private void ReturnResponse(IEnumerable<PeerId> peers)
        {
            var response = new GetPeerListResponse();
            response.Peers.AddRange(peers);

            var anySignedResponse = response.ToAnySigned(_peerId, _message.Payload.CorrelationId.ToGuid());

            _message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}
