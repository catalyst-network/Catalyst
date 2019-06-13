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

using System;
using System.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class PeerReputationRequestMessageObserver
        : RequestMessageObserverBase<GetPeerReputationRequest>,
            IRpcRequestMessageObserver
    {
        /// <summary>
        /// The PeerReputationRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;

        public PeerReputationRequestMessageObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRepository<Peer> peerRepository)
            : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type PeerReputationRequest");

            Guard.Argument(messageDto, nameof(messageDto)).NotNull("Received message cannot be null");

            var deserialised = messageDto.Payload.FromProtocolMessage<GetPeerReputationRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8() ?? throw new ArgumentNullException(nameof(messageDto));
            
            var ip = deserialised.Ip.ToStringUtf8();

            return new GetPeerReputationResponse
            {
                Reputation = _peerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString()
                     && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey)
                   .Select(x => x.Reputation).DefaultIfEmpty(int.MinValue).First()
            };
        }
    }
}


