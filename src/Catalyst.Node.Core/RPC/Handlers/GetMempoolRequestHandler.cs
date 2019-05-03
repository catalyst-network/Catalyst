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
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class GetMempoolRequestHandler
        : CorrelatableMessageHandlerBase<GetMempoolRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        private readonly IMempool _mempool;
        private readonly IPeerIdentifier _peerIdentifier;

        public GetMempoolRequestHandler(IPeerIdentifier peerIdentifier,
            IMempool mempool,
            IMessageCorrelationCache messageCorrelationCache,
            ILogger logger)
            : base(messageCorrelationCache, logger)
        {
            _mempool = mempool;
            _peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull();
            
            Logger.Debug("GetMempoolRequestHandler starting ...");
            
            try
            {
                var deserialised = message.Payload.FromAnySigned<GetMempoolRequest>();

                Guard.Argument(deserialised).NotNull("The shell GetMempoolRequest cannot be null.");
                
                Logger.Debug("Received GetMempoolRequest message with content {0}", deserialised);

                var response = new RpcMessageFactory<GetMempoolResponse>().GetMessage(
                    new GetMempoolResponse
                    {
                        Mempool = {GetMempoolContent()}
                    },
                    new PeerIdentifier(message.Payload.PeerId),
                    _peerIdentifier,
                    DtoMessageType.Tell,
                    message.Payload.CorrelationId.ToGuid());
                
                message.Context.Channel.WriteAndFlushAsync(response).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", message);
                throw;
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
                    "Failed to get the mempool content and format it as MapField<string,string> {0}", ex.Message);
                throw;
            }
        }
    }
}
