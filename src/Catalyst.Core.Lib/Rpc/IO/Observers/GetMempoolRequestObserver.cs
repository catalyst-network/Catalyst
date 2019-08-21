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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Lib.Rpc.IO.Observers
{
    public sealed class GetMempoolRequestObserver
        : RequestObserverBase<GetMempoolRequest, GetMempoolResponse>,
            IRpcRequestObserver
    {
        private readonly IMempool _mempool;

        public GetMempoolRequestObserver(IPeerIdentifier peerIdentifier,
            IMempool mempool,
            ILogger logger)
            : base(logger, peerIdentifier)
        {
            _mempool = mempool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="getMempoolRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetMempoolResponse HandleRequest(GetMempoolRequest getMempoolRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getMempoolRequest, nameof(getMempoolRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("GetMempoolRequestHandler starting ...");

            try
            {
                Logger.Debug("Received GetMempoolRequest message with content {0}", getMempoolRequest);

                var mempoolTransactions = _mempool.GetMemPoolContentAsTransactions();

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
