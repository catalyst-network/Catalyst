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

using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Node.Rpc.Client.Observables
{
    /// <summary>
    /// The Transfer file bytes response handler
    /// </summary>
    /// <seealso cref="IRpcResponseMessageObserver" />
    public class TransferFileBytesResponseMessageObserver
        : ResponseMessageObserverBase<TransferFileBytesResponse>,
            IRpcResponseMessageObserver
    {
        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesResponseMessageObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesResponseMessageObserver(ILogger logger) : base(logger) { }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            // Response for a node writing a chunk via bytes transfer.
            // Future logic if an error occurs via chunk transfer then preferably we want to stop file transfer
        }
    }
}
