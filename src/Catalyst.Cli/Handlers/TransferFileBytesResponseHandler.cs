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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// The Transfer file bytes response handler
    /// </summary>
    /// <seealso cref="IRpcResponseHandler" />
    public class TransferFileBytesResponseHandler
        : MessageHandlerBase<TransferFileBytesResponse>,
            IRpcResponseHandler
    {
        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesResponseHandler"/> class.</summary>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesResponseHandler(ILogger logger) : base(logger) { }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            // Response for a node writing a chunk via bytes transfer.
            // Future logic if an error occurs via chunk transfer then preferably we want to stop file transfer
        }
    }
}
