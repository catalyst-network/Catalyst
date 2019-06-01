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
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;
using Dawn;
using Google.Protobuf;
using Nethereum.RLP;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class PeerBlackListingRequestHandler
        : CorrelatableMessageHandlerBase<SetPeerBlackListRequest, IRpcCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>
        /// The PeerBlackListingRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;

        private IChanneledMessage<AnySigned> _message;
        
        private readonly PeerId _peerId;

        public PeerBlackListingRequestHandler(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRpcCorrelationCache messageCorrelationCache,
            IRepository<Peer> peerRepository)
            : base(messageCorrelationCache, logger)
        {
            _peerId = peerIdentifier.PeerId;
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            _message = message;

            var deserialised = message.Payload.FromAnySigned<SetPeerBlackListRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8(); 
            var ip = deserialised.Ip.ToStringUtf8();
            var blackList = deserialised.Blacklist;

            var peerItem = _peerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString() 
             && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey).FirstOrDefault();

            if (peerItem != null)
            {
                peerItem.BlackListed = blackList;
                ReturnResponse(blackList, deserialised.PublicKey, deserialised.Ip, message);
            }
            else
            {
                ReturnResponse(false, string.Empty.ToUtf8ByteString(), string.Empty.ToUtf8ByteString(), message);
            }

            Logger.Debug("received message of type PeerBlackListingRequest");
        }

        /// <summary>
        /// Returns the response.
        /// </summary>
        /// <param name="blacklist">if set to <c>true</c> [blacklist].</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="ip">The ip.</param>
        /// <param name="message">The message.</param>
        private void ReturnResponse(bool blacklist, ByteString publicKey, ByteString ip, IChanneledMessage<AnySigned> message)
        {
            var response = new SetPeerBlackListResponse
            {
                Blacklist = blacklist,
                Ip = ip,
                PublicKey = publicKey
            };

            var anySignedResponse = response.ToAnySigned(_peerId, _message.Payload.CorrelationId.ToGuid());

            message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
        }
    }
}


