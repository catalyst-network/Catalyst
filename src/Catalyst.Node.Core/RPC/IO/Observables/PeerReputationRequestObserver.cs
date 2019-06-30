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
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Nethereum.RLP;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.IO.Observables
{
    public sealed class PeerReputationRequestObserver
        : RequestObserverBase<GetPeerReputationRequest, GetPeerReputationResponse>,
            IRpcRequestObserver
    {
        /// <summary>
        /// The PeerReputationRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;

        public PeerReputationRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRepository<Peer> peerRepository)
            : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="getPeerReputationRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetPeerReputationResponse HandleRequest(GetPeerReputationRequest getPeerReputationRequest, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, Guid correlationId)
        {
            Guard.Argument(getPeerReputationRequest, nameof(getPeerReputationRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("received message of type PeerReputationRequest");
            
            var ip = getPeerReputationRequest.Ip.ToStringUtf8();

            return new GetPeerReputationResponse
            {
                Reputation = _peerRepository.GetAll().Where(m => m.PeerIdentifier.Ip.ToString() == ip.ToString()
                     && ConvertorForRLPEncodingExtensions.ToStringFromRLPDecoded(m.PeerIdentifier.PublicKey) == getPeerReputationRequest.PublicKey.ToStringUtf8())
                   .Select(x => x.Reputation).DefaultIfEmpty(int.MinValue).First()
            };
        }
    }
}


