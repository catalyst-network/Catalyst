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
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Handles the Peer reputation response
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class GetPeerInfoResponseObserver
        : ResponseObserverBase<GetPeerInfoResponse>,
            IRpcResponseObserver
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerReputationResponseObserver"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="logger">The logger.</param>
        public GetPeerInfoResponseObserver(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
        }

        /// <summary>
        /// Handle the response for GetPeerInfo
        /// </summary>
        /// <param name="getPeerInfoResponse">The response</param>
        /// <param name="channelHandlerContext">The channel handler context</param>
        /// <param name="senderPeerIdentifier">The sender peer identifier</param>
        /// <param name="correlationId">The correlationId</param>
        protected override void HandleResponse(GetPeerInfoResponse getPeerInfoResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getPeerInfoResponse, nameof(getPeerInfoResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("Handling GetPeerInfo response");

            try
            {
                var msg = getPeerInfoResponse.PeerInfo.Count == 0 ? "Peer not found" : "GetPeerInfo Successful";
                _output.WriteLine(msg);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetPeerInfoResponse after receiving message {0}", getPeerInfoResponse);
                throw;
            }
        }
    }
}
