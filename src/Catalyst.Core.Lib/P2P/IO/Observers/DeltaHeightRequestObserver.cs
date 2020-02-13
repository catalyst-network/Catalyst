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
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Lib.Service;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class DeltaHeightRequestObserver
        : RequestObserverBase<LatestDeltaHashRequest, LatestDeltaHashResponse>,
            IP2PMessageObserver
    {
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;
        private readonly SyncState _syncState;

        public DeltaHeightRequestObserver(IPeerSettings peerSettings,
            IDeltaIndexService deltaIndexService,
            IMapperProvider mapperProvider,
            SyncState syncState,
            ILogger logger)
            : base(logger, peerSettings)
        {
            _deltaIndexService = deltaIndexService;
            _mapperProvider = mapperProvider;
            _syncState = syncState;
        }

        /// <param name="deltaHeightRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override LatestDeltaHashResponse HandleRequest(LatestDeltaHashRequest deltaHeightRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(deltaHeightRequest, nameof(deltaHeightRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();

            Logger.Debug("PeerId: {0} wants to know your current chain height", senderPeerId);

            var deltaIndexDao = _deltaIndexService.LatestDeltaIndex();
            var deltaIndex = deltaIndexDao.ToProtoBuff<DeltaIndexDao, DeltaIndex>(_mapperProvider);
            return new LatestDeltaHashResponse
            {
                IsSync = _syncState.IsSynchronized,
                DeltaIndex = deltaIndex
            };
        }
    }
}
