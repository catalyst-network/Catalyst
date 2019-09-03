
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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Observers;
using Catalyst.Core.P2P.Repository;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Nethereum.RLP;
using Serilog;

namespace Catalyst.Core.Rpc.IO.Observers
{
    public sealed class PeerBlackListingRequestObserver
        : RequestObserverBase<SetPeerBlackListRequest, SetPeerBlackListResponse>,
            IRpcRequestObserver
    {
        /// <summary>
        /// The PeerBlackListingRequestHandler 
        /// </summary>
        private readonly IPeerRepository _peerRepository;
        
        public PeerBlackListingRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IPeerRepository peerRepository)
            : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="setPeerBlackListRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override SetPeerBlackListResponse HandleRequest(SetPeerBlackListRequest setPeerBlackListRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(setPeerBlackListRequest, nameof(setPeerBlackListRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Information("received message of type PeerBlackListingRequest");
            
            var peerItem = _peerRepository.GetAll().FirstOrDefault(m => m.PeerIdentifier.Ip.ToString() == setPeerBlackListRequest.Ip.ToStringUtf8() 
             && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == setPeerBlackListRequest.PublicKey.ToStringUtf8());

            return peerItem == null
                ? ReturnResponse(false, string.Empty.ToUtf8ByteString(), string.Empty.ToUtf8ByteString()) 
                : ReturnResponse(setPeerBlackListRequest.Blacklist, setPeerBlackListRequest.PublicKey, setPeerBlackListRequest.Ip);
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
