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
using System.Text;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
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

        protected override IMessage<GetMempoolResponse> HandleRequest(IInboundDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("GetMempoolRequestHandler starting ...");

            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<GetMempoolRequest>();
                
                Logger.Debug("Received GetMempoolRequest message with content {0}", deserialised);

                return new GetMempoolResponse
                {
                    Mempool = {GetMempoolContent()}
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", messageDto);
                return new GetMempoolResponse();
            }
        }

        private IEnumerable<string> GetMempoolContent()
        {
            var mempoolList = new List<string>();

            try
            {
                var memPoolContentEncoded = _mempool.GetMemPoolContentEncoded();

                foreach (var tx in memPoolContentEncoded)
                {
                    mempoolList.Add(Encoding.Default.GetString(tx));
                }

                return mempoolList;
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to get the mempool content and format it as List<string> {0}", ex.Message);
                throw;
            }
        }
    }
}
