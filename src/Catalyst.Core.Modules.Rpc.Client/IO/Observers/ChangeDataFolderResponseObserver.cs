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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Lib.Rpc.IO;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Observers
{
    /// <summary>
    ///     Handles the Peer count response
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class ChangeDataFolderResponseObserver
        : RpcResponseObserver<SetPeerDataFolderResponse>
    {
        public ChangeDataFolderResponseObserver(ILogger logger)
            : base(logger) { }

        protected override void HandleResponse(SetPeerDataFolderResponse setPeerDataFolderResponse,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            //Overriden code
        }
    }
}
