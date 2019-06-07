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
using Dawn;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// The response handler for removing a peer
    /// </summary>
    /// <seealso cref="IRpcResponseHandler" />
    public sealed class RemovePeerResponseHandler
        : MessageHandlerBase<RemovePeerResponse>,
            IRpcResponseHandler
    {
        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerResponseHandler"/> class.</summary>
        /// <param name="userOutput">The user output.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerResponseHandler(IUserOutput userOutput,
            ILogger logger) : base(logger)
        {
            _userOutput = userOutput;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<ProtocolMessage> message)
        {
            Logger.Debug("Handling Remove Peer Response");

            Guard.Argument(message).NotNull("Received message cannot be null");

            try
            {
                var deserialised = message.Payload.FromAnySigned<RemovePeerResponse>();
                var deletedCount = deserialised.DeletedCount;

                _userOutput.WriteLine($"Deleted {deletedCount.ToString()} peers");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle RemovePeerResponse after receiving message {0}", message);
                throw;
            }
        }
    }
}
