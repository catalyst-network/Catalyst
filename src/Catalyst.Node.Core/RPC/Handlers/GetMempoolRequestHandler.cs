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
using System.Text;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf.Collections;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public sealed class GetMempoolRequestHandler : MessageHandlerBase<GetMempoolRequest>
    {
        private readonly IMempool _mempool;
        private readonly PeerId _peerId;

        public GetMempoolRequestHandler(IObservable<IChanneledMessage<AnySigned>> messageStream,
            IPeerIdentifier peerIdentifier,
            ILogger logger,
            IMempool mempool)
            : base(messageStream, logger)
        {
            _mempool = mempool;
            _peerId = peerIdentifier.PeerId;
        }

        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            if (message == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            Logger.Debug("received message of type GetMempoolRequest");
            try
            {
                var deserialised = message.Payload.FromAnySigned<GetMempoolRequest>();
                Logger.Debug("message content is {0}", deserialised);
                var response = new GetMempoolResponse
                {
                    Info =
                    {
                        GetMempoolContent()
                    }
                };

                var anySignedResponse = response.ToAnySigned(_peerId, message.Payload.CorrelationId.ToGuid());
                message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", message);
                throw;
            }
        }

        private MapField<string, string> GetMempoolContent()
        {
            var memPoolContentEncoded = _mempool.GetMemPoolContentEncoded();
            var memPoolMap = new MapField<string, string>();

            for (var i = 0; i < memPoolContentEncoded.Count; i++)
            {
                var sb = new StringBuilder("{");
                foreach (var b in memPoolContentEncoded[i])
                {
                    sb.Append(b);
                }

                sb.Append("}");

                memPoolMap.Add(i.ToString(), sb.ToString());
            }

            return memPoolMap;
        }
    }
}
