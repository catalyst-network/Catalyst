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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class GetMempoolRequestObserver
        : RequestObserverBase<GetMempoolRequest, GetMempoolResponse>,
            IRpcRequestObserver
    {
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IMapperProvider _mappingProvider;

        public GetMempoolRequestObserver(IPeerSettings peerSettings,
            IMempool<PublicEntryDao> mempool,
            IMapperProvider mappingProvider,
            ILogger logger)
            : base(logger, peerSettings)
        {
            _mempool = mempool;
            _mappingProvider = mappingProvider;
        }

        /// <summary>
        /// </summary>
        /// <param name="getMempoolRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetMempoolResponse HandleRequest(GetMempoolRequest getMempoolRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(getMempoolRequest, nameof(getMempoolRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("GetMempoolRequestHandler starting ...");

            try
            {
                Logger.Debug("Received GetMempoolRequest message with content {0}", getMempoolRequest);

                var mempoolTransactions = _mempool.Service.GetAll()
                   .Select(x => x.ToProtoBuff<PublicEntryDao, PublicEntry>(_mappingProvider));

                return new GetMempoolResponse
                {
                    Transactions = {mempoolTransactions}
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", getMempoolRequest);
                return new GetMempoolResponse();
            }
        }
    }
}
