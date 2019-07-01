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
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.IO.Observables
{
    public sealed class GetInfoRequestObserver
        : RequestObserverBase<GetInfoRequest, GetInfoResponse>,
            IRpcRequestObserver
    {
        private readonly IRpcServerSettings _config;

        public GetInfoRequestObserver(IPeerIdentifier peerIdentifier,
            IRpcServerSettings config,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _config = config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="getInfoRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override GetInfoResponse HandleRequest(GetInfoRequest getInfoRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            Guid correlationId)
        {
            Guard.Argument(getInfoRequest, nameof(getInfoRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("received message of type GetInfoRequest");
            
            try
            {
                Logger.Debug("message content is {0}", getInfoRequest);

                var serializedList = JsonConvert.SerializeObject(
                    _config.NodeConfig.GetSection("CatalystNodeConfiguration").AsEnumerable(), 
                    Formatting.Indented);
                
                return new GetInfoResponse
                {
                    Query = serializedList
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", getInfoRequest);
                throw;
            }
        }
    }
}
