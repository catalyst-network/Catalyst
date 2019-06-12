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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// Peer count request handler
    /// </summary>
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class PeerCountRequestHandler
        : MessageHandlerBase<GetPeerCountRequest>,
            IRpcRequestHandler
    {
        /// <summary>The peer discovery</summary>
        private readonly IRepository<Peer> _peerRepository;

        /// <summary>The RPC message base</summary>
        private readonly IMessageFactory _messageFactory;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="PeerCountRequestHandler"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="messageFactory"></param>
        /// <param name="logger">The logger.</param>
        public PeerCountRequestHandler(IPeerIdentifier peerIdentifier,
            IRepository<Peer> peerRepository,
            IMessageFactory messageFactory,
            ILogger logger) :
            base(logger)
        {
            _peerRepository = peerRepository;
            _messageFactory = messageFactory;
            _peerIdentifier = peerIdentifier;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<ProtocolMessage> message)
        {
            var peerCount = _peerRepository.GetAll().Count();

            var response = new GetPeerCountResponse
            {
                PeerCount = peerCount
            };

            var responseMessage = _messageFactory.GetMessage(new MessageDto(
                    response,
                    MessageTypes.Response,
                    new PeerIdentifier(message.Payload.PeerId),
                    _peerIdentifier),
                message.Payload.CorrelationId.ToGuid()
            );

            message.Context.Channel.WriteAndFlushAsync(responseMessage);
        }
    }
}
