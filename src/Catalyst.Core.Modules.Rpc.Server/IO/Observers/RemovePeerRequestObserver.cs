#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;
using MultiFormats;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Rpc.IO.Observers;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    /// <summary>
    ///     Remove Peer handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class RemovePeerRequestObserver
        : RpcRequestObserverBase<RemovePeerRequest, RemovePeerResponse>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IPeerRepository _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestObserver" /> class.</summary>
        /// <param name="peerSettings"></param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerRequestObserver(IPeerSettings peerSettings,
            IPeerRepository peerRepository,
            ILogger logger) : base(logger, peerSettings)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="removePeerRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="sender"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override RemovePeerResponse HandleRequest(RemovePeerRequest removePeerRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            Guard.Argument(removePeerRequest, nameof(removePeerRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Logger.Debug("Received message of type RemovePeerRequest");

            var peerDeletedCount = _peerRepository.DeletePeersByAddress(removePeerRequest.Address);

            return new RemovePeerResponse
            {
                DeletedCount = peerDeletedCount
            };
        }
    }
}
