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

using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.IO.Observers;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using ILogger = Serilog.ILogger;

namespace Catalyst.Core.Lib.Rpc.IO.Observers
{
    public sealed class ChangeDataFolderRequestObserver
        : RequestObserverBase<SetPeerDataFolderRequest, GetPeerDataFolderResponse>,
            IRpcRequestObserver
    {
        private readonly IFileSystem _fileSystem;

        public ChangeDataFolderRequestObserver(IPeerIdentifier peerIdentifier,
            IRpcServerSettings config, 
            IFileSystem fileSystem,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _fileSystem = fileSystem;
        }

        protected override GetPeerDataFolderResponse HandleRequest(SetPeerDataFolderRequest setDataFolderRequest,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(setDataFolderRequest, nameof(setDataFolderRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();

            Logger.Debug("received message of type SetPeerDataFolderRequest");

            return new GetPeerDataFolderResponse
            {
                Query = _fileSystem.SetCurrentPath(setDataFolderRequest.Datafolder)
            };
        }
    }
}
