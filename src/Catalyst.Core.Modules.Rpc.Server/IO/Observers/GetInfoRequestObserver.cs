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
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Lib.P2P.Protocols;
using Microsoft.Extensions.Configuration;
using MultiFormats;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Server.IO.Observers
{
    public sealed class GetInfoRequestObserver
        : RequestObserverBase<GetInfoRequest, GetInfoResponse>,
            IRpcRequestObserver
    {
        private readonly IConfigurationRoot _config;

        public GetInfoRequestObserver(IPeerSettings peerSettings,
            ILibP2PPeerClient peerClient,
            IConfigurationRoot config,
            ILogger logger) : base(logger, peerSettings, peerClient)
        {
            _config = config;
        }

        protected override GetInfoResponse HandleRequest(GetInfoRequest getInfoRequest,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(getInfoRequest, nameof(getInfoRequest)).NotNull();
            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            Logger.Debug("received message of type GetInfoRequest");

            Logger.Debug("message content is {0}", getInfoRequest);

            // @TODO not sure why we serialise this server side?
            var serializedList = JsonConvert.SerializeObject(
                _config.GetSection("CatalystNodeConfiguration").AsEnumerable(),
                Formatting.Indented);

            return new GetInfoResponse
            {
                Query = serializedList
            };
        }
    }
}
