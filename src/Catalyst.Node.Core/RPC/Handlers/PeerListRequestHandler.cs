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
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using System.Collections.Generic;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Common.Rpc;
using Dawn;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    ///     Handles the PeerListRequest message
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetPeerListRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class PeerListRequestHandler
        : CorrelatableMessageHandlerBase<GetPeerListRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>
        ///     repository interface to storage
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;
        
        /// <summary>
        ///     The peer identifier
        /// </summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>
        ///     The RPC message factory
        /// </summary>
        private readonly IRpcMessageFactory _rpcMessageFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerListRequestHandler"/> class.
        /// </summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="messageCorrelationCache">The message correlation cache.</param>
        /// <param name="peerRepository"></param>
        /// <param name="rpcMessageFactory"></param>
        public PeerListRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRpcCorrelationCache messageCorrelationCache,
            IRepository<Peer> peerRepository,
            IRpcMessageFactory rpcMessageFactory)
            : base(messageCorrelationCache, logger)
        {
            _peerIdentifier = peerIdentifier;
            _peerRepository = peerRepository;
            _rpcMessageFactory = rpcMessageFactory;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull("Received message cannot be null");

            ReturnResponse(_peerRepository.GetAll().Select(x => x.PeerIdentifier.PeerId), message);

            Logger.Debug("received message of type PeerListRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="peers">The peers list</param>
        /// <param name="message"></param>
        private void ReturnResponse(IEnumerable<PeerId> peers, IChanneledMessage<AnySigned> message)
        {
            var response = new GetPeerListResponse();
            response.Peers.AddRange(peers);
            
            var responseMessage = _rpcMessageFactory.GetMessage(new MessageDto(
                    response,
                    MessageTypes.Tell,
                    new PeerIdentifier(message.Payload.PeerId),
                    _peerIdentifier
                ),
                message.Payload.CorrelationId.ToGuid()
            );

            message.Context.Channel.WriteAndFlushAsync(responseMessage).GetAwaiter().GetResult();
        }
    }
}
