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
using AutoMapper;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class DeltaHistoryRequestObserver 
        : RequestObserverBase<DeltaHistoryRequest, DeltaHistoryResponse>,
            IP2PMessageObserver
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IMapperProvider _mapperProvider;

        public DeltaHistoryRequestObserver(IPeerSettings peerSettings,
            IDeltaIndexService deltaIndexService,
            ILogger logger,
            IHashProvider hashProvider,
            IMapperProvider mapperProvider)
            : base(logger, peerSettings)
        {
            _deltaIndexService = deltaIndexService;
            _hashProvider = hashProvider;
            _mapperProvider = mapperProvider;
        }
        
        /// <param name="deltaHeightRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override DeltaHistoryResponse HandleRequest(DeltaHistoryRequest deltaHeightRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(deltaHeightRequest, nameof(deltaHeightRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            
            Logger.Debug("PeerId: {0} requests: {1} deltas from height: {2}", senderPeerId, deltaHeightRequest.Range, deltaHeightRequest.Height);

            var count = deltaHeightRequest.Height + deltaHeightRequest.Range;
            var range = _deltaIndexService.GetRange((int) deltaHeightRequest.Height, (int) count);
            var rangeDao = range.Select(x => x.ToProtoBuff<DeltaIndexDao, DeltaIndex>(_mapperProvider));
            var response = new DeltaHistoryResponse();
            response.Result.Add(rangeDao);
            return response;
        }
    }
}
