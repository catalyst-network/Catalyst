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
using Google.Protobuf;
using Nethereum.RLP;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class PeerBlackListingRequestObserver
        : ObserverBase<SetPeerBlackListRequest>,
            IRpcRequestObserver
    {
        /// <summary>
        /// The PeerBlackListingRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;

        private IProtocolMessageDto<ProtocolMessage> _messageDto;
        
        private readonly PeerId _peerId;

        public PeerBlackListingRequestObserver(IPeerIdentifier peerIdentifier,
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
            _messageDto = messageDto;

            var deserialised = messageDto.Payload.FromProtocolMessage<SetPeerBlackListRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8(); 
            var ip = deserialised.Ip.ToStringUtf8();
            var blackList = deserialised.Blacklist;

            var peerItem = _peerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString() 
             && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey).FirstOrDefault();

            if (peerItem != null)
            {
                peerItem.BlackListed = blackList;
                ReturnResponse(blackList, deserialised.PublicKey, deserialised.Ip, messageDto);
            }
            else
            {
                ReturnResponse(false, string.Empty.ToUtf8ByteString(), string.Empty.ToUtf8ByteString(), messageDto);
            }

            Logger.Debug("received message of type PeerBlackListingRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="blacklist">if set to <c>true</c> [blacklist].</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="ip">The ip.</param>
        /// <param name="messageDto">The message.</param>
        private void ReturnResponse(bool blacklist, ByteString publicKey, ByteString ip, IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            var response = new SetPeerBlackListResponse
            {
                Blacklist = blacklist,
                Ip = ip,
                PublicKey = publicKey
            };

            var anySignedResponse = response.ToProtocolMessage(_peerId, _messageDto.Payload.CorrelationId.ToGuid());

            messageDto.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}


