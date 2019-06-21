
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
    public sealed class PeerBlackListingRequestObserver
        : RequestObserverBase<SetPeerBlackListRequest, SetPeerBlackListResponse>,
            IRpcRequestObserver
    {
        /// <summary>
        /// The PeerBlackListingRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;
        
        public PeerBlackListingRequestObserver(IPeerIdentifier peerIdentifier,
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
        public override IMessage<SetPeerBlackListResponse> HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Information("received message of type PeerBlackListingRequest");

            var deserialised = messageDto.Payload.FromProtocolMessage<SetPeerBlackListRequest>();

            var peerItem = _peerRepository.GetAll().FirstOrDefault(m => m.PeerIdentifier.Ip.ToString() == deserialised.Ip.ToStringUtf8() 
             && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == deserialised.PublicKey.ToStringUtf8());

            return peerItem == null ? ReturnResponse(false, string.Empty.ToUtf8ByteString(), string.Empty.ToUtf8ByteString()) : ReturnResponse(deserialised.Blacklist, deserialised.PublicKey, deserialised.Ip);
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="blacklist">if set to <c>true</c> [blacklist].</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="ip">The ip.</param>
        private SetPeerBlackListResponse ReturnResponse(bool blacklist, ByteString publicKey, ByteString ip)
        {
            return new SetPeerBlackListResponse
            {
                Blacklist = blacklist,
                Ip = ip,
                PublicKey = publicKey
            };
        }
    }
}
