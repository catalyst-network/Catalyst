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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.IO.Observables
{
    /// <summary>
    /// Handles the Peer list response from the node
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class PeerListResponseObserver
        : ResponseObserverBase<GetPeerListResponse>,
            IRpcResponseObserver
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListResponseObserver"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="logger">The logger.</param>
        public PeerListResponseObserver(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
        }

        /// <summary>
        /// Handles the peer list response.
        /// </summary>
        /// <param name="messageDto">The PeerListResponse message.</param>
        public override void HandleResponse(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("Handling PeerListResponse");

            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<GetPeerListResponse>() ?? throw new ArgumentNullException(nameof(messageDto));
                var result = string.Join(", ", deserialised.Peers);
                _output.WriteLine(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle PeerListResponse after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
