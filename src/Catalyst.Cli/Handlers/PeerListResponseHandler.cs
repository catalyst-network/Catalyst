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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;
using Dawn;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handles the Peer list response from the node
    /// </summary>
    /// <seealso cref="IRpcResponseHandler" />
    public sealed class PeerListResponseHandler
        : MessageHandlerBase<GetPeerListResponse>,
            IRpcResponseHandler
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListResponseHandler"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="logger">The logger.</param>
        public PeerListResponseHandler(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
        }

        /// <summary>
        /// Handles the peer list response.
        /// </summary>
        /// <param name="message">The PeerListResponse message.</param>
        protected override void Handler(IChanneledMessage<ProtocolMessage> message)
        {
            Logger.Debug("Handling PeerListResponse");
            Guard.Argument(message).NotNull("Received message cannot be null");

            try
            {
                var deserialised = message.Payload.FromAnySigned<GetPeerListResponse>();
                var result = string.Join(", ", deserialised.Peers);
                _output.WriteLine(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle PeerListResponse after receiving message {0}", message);
                throw;
            }
        }
    }
}
