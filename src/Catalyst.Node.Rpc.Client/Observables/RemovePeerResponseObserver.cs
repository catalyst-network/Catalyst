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
using Serilog;

namespace Catalyst.Node.Rpc.Client.Observables
{
    /// <summary>
    /// The response handler for removing a peer
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class RemovePeerResponseObserver
        : ResponseObserverBase<RemovePeerResponse>,
            IRpcResponseObserver
    {
        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerResponseObserver"/> class.</summary>
        /// <param name="userOutput">The user output.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerResponseObserver(IUserOutput userOutput,
            ILogger logger) : base(logger)
        {
            _userOutput = userOutput;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug($@"Handling Remove Peer Response");
            
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<RemovePeerResponse>() ?? throw new ArgumentNullException(nameof(messageDto));
                var deletedCount = deserialised.DeletedCount;

                _userOutput.WriteLine($@"Deleted {deletedCount.ToString()} peers");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle RemovePeerResponse after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
