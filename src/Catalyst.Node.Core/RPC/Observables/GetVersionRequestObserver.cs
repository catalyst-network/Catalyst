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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class GetVersionRequestObserver
        : RequestObserverBase<VersionRequest>,
            IRpcRequestObserver
    {
        public GetVersionRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger)
            : base(logger, peerIdentifier) { }

        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type VersionRequest");

            try
            {
                return new VersionResponse
                {
                    Version = NodeUtil.GetVersion()
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetVersionRequest after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
