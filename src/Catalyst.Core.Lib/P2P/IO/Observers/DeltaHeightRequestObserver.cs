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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class DeltaHeightRequestObserver 
        : RequestObserverBase<DeltaHeightRequest, DeltaHeightResponse>,
            IP2PMessageObserver
    {
        public DeltaHeightRequestObserver(IPeerSettings peerSettings,
            ILogger logger)
            : base(logger, peerSettings) { }
        
        /// <param name="pingRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override DeltaHeightResponse HandleRequest(DeltaHeightRequest pingRequest, IChannelHandlerContext channelHandlerContext, PeerId senderPeerId, ICorrelationId correlationId)
        {
            Guard.Argument(pingRequest, nameof(pingRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            
            Logger.Debug("PeerId: {0} wants to know your current chain height", senderPeerId);

            return new DeltaHeightResponse
            {
                DeltaHash = Multihash.Sum<BLAKE2B_256>(new byte[32]).ToBytes().ToByteString() // @TODO get from hashing module
            };
        }
    }
}
