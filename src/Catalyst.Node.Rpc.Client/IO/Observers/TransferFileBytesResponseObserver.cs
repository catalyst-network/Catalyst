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

using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// The Transfer file bytes response handler
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public class TransferFileBytesResponseObserver
        : ResponseObserverBase<TransferFileBytesResponse>,
            IRpcResponseObserver
    {
        /// <summary>Initializes a new instance of the <see cref="TransferFileBytesResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        public TransferFileBytesResponseObserver(ILogger logger) : base(logger) { }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transferFileBytesResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(TransferFileBytesResponse transferFileBytesResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(transferFileBytesResponse, nameof(transferFileBytesResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            
            // Response for a node writing a chunk via bytes transfer.
            // Future logic if an error occurs via chunk transfer then preferably we want to stop file transfer
        }
    }
}
