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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Lib.Rpc.IO.Observers
{
    /// <summary>
    /// Remove Peer handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class RemovePeerRequestObserver
        : RequestObserverBase<RemovePeerRequest, RemovePeerResponse>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IRepository<Peer> _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestObserver"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerRequestObserver(IPeerIdentifier peerIdentifier,
            IRepository<Peer> peerRepository,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="removePeerRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override RemovePeerResponse HandleRequest(RemovePeerRequest removePeerRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(removePeerRequest, nameof(removePeerRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("Received message of type RemovePeerRequest");

            uint peerDeletedCount = 0;

            var publicKeyIsEmpty = removePeerRequest.PublicKey.IsEmpty;
            
            var peersToDelete = _peerRepository.GetAll().TakeWhile(peer =>
                peer.PeerIdentifier.Ip.To16Bytes().SequenceEqual(removePeerRequest.PeerIp.ToByteArray()) &&
                (publicKeyIsEmpty || peer.PeerIdentifier.PublicKey.SequenceEqual(removePeerRequest.PublicKey.ToByteArray()))).ToArray();

            foreach (var peerToDelete in peersToDelete)
            {
                _peerRepository.Delete(peerToDelete);
                peerDeletedCount += 1;
            }

            return new RemovePeerResponse
            {
                DeletedCount = peerDeletedCount
            };
        }
    }
}
