#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Core.Lib.Rpc.IO;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Observers
{
    /// <summary>
    ///     Handler responsible for handling the server's response for the GetInfo request.
    ///     The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class GetInfoResponseObserver
        : RpcResponseObserver<GetInfoResponse>
    {
        /// <summary>
        /// </summary>
        /// <param name="logger">Logger to log debug related information.</param>
        public GetInfoResponseObserver(ILogger logger)
            : base(logger) { }

        /// <summary>
        /// </summary>
        /// <param name="getInfoResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(GetInfoResponse getInfoResponse,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerIdentifier,
            ICorrelationId correlationId) { }
    }
}
