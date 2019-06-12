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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Nethereum.RLP;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class PeerReputationRequestObserver
        : ObserverBase,
            IRpcRequestObserver
    {
        /// <summary>
        /// The PeerReputationRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;

        private IProtocolMessageDto<ProtocolMessage> _messageDto;
        
        private readonly PeerId _peerId;

        public PeerReputationRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRepository<Peer> peerRepository)
            : base(logger)
        {
            _peerId = peerIdentifier.PeerId;
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="messageDto">The message.</param>
        protected override void Handler(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto).NotNull("Received message cannot be null");

            _messageDto = messageDto;

            var deserialised = messageDto.Payload.FromProtocolMessage<GetPeerReputationRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8(); 
            var ip = deserialised.Ip.ToStringUtf8();

            ReturnResponse(_peerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString()
                 && ConvertorForRLPEncodingExtensions.ToStringFromRLPDecoded(m.PeerIdentifier.PublicKey) == publicKey)
               .Select(x => x.Reputation).DefaultIfEmpty(int.MinValue).First(), messageDto);

            Logger.Debug("received message of type PeerReputationRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="reputation"></param>
        /// <param name="messageDto"></param>
        private void ReturnResponse(int reputation, IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            var response = new GetPeerReputationResponse
            {
                Reputation = reputation
            };

            var anySignedResponse = response.ToProtocolMessage(_peerId, _messageDto.Payload.CorrelationId.ToGuid());

            messageDto.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}


