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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    ///     Handles the PeerListRequest message
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class PeerListRequestObserver
        : ObserverBase<GetPeerListRequest>,
            IRpcRequestObserver
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
        private readonly IMessageFactory _messageFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerListRequestObserver"/> class.
        /// </summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="peerRepository"></param>
        /// <param name="messageFactory"></param>
        public PeerListRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRepository<Peer> peerRepository,
            IMessageFactory messageFactory)
            : base(logger)
        {
            _peerIdentifier = peerIdentifier;
            _peerRepository = peerRepository;
            _messageFactory = messageFactory;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="messageDto">The message.</param>
        protected override void Handler(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto).NotNull("Received message cannot be null");

            ReturnResponse(_peerRepository.GetAll().Select(x => x.PeerIdentifier.PeerId), messageDto);

            Logger.Debug("received message of type PeerListRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="peers">The peers list</param>
        /// <param name="messageDto"></param>
        private void ReturnResponse(IEnumerable<PeerId> peers, IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            var response = new GetPeerListResponse();
            response.Peers.AddRange(peers);
            
            var responseMessage = _messageFactory.GetMessage(new MessageDto(
                    response,
                    MessageTypes.Response,
                    new PeerIdentifier(messageDto.Payload.PeerId),
                    _peerIdentifier
                ),
                messageDto.Payload.CorrelationId.ToGuid()
            );

            messageDto.Context.Channel.WriteAndFlushAsync(responseMessage).GetAwaiter().GetResult();
        }
    }
}
