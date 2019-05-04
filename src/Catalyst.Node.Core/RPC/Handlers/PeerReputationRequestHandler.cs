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
using Dawn;
using Nethereum.RLP;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class PeerReputationRequestHandler
        : CorrelatableMessageHandlerBase<GetPeerReputationRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>
        /// The PeerReputationRequestHandler 
        /// </summary>
        private readonly IPeerDiscovery _peerDiscovery;

        private IChanneledMessage<AnySigned> _message;
        
        private readonly PeerId _peerId;

        public PeerReputationRequestHandler(IPeerIdentifier peerIdentifier,
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
            Guard.Argument(message).NotNull("Received message cannot be null");

            _message = message;

            var deserialised = message.Payload.FromAnySigned<GetPeerReputationRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8(); 
            var ip = deserialised.Ip.ToStringUtf8();

            ReturnResponse(_peerDiscovery.PeerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString()
                 && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey)
               .Select(x => x.Reputation).DefaultIfEmpty(int.MinValue).First(), message);

            Logger.Debug("received message of type PeerReputationRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="reputation"></param>
        /// <param name="message"></param>
        private void ReturnResponse(int reputation, IChanneledMessage<AnySigned> message)
        {
            var response = new GetPeerReputationResponse
            {
                Reputation = reputation
            };

            var anySignedResponse = response.ToAnySigned(_peerId, _message.Payload.CorrelationId.ToGuid());

            message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}


