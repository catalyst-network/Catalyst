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

using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.Rpc.IO.Observers
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
            ICorrelationId correlationId)
        {
            Guard.Argument(getInfoRequest, nameof(getInfoRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("received message of type GetInfoRequest");

            Logger.Debug("message content is {0}", getInfoRequest);

            var serializedList = JsonConvert.SerializeObject(
                _config.NodeConfig.GetSection("CatalystNodeConfiguration").AsEnumerable(),
                Formatting.Indented);

            return new GetInfoResponse
            {
                Query = serializedList
            };
        }
    }
}
