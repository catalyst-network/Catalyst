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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    public class GetVersionRequestHandler : MessageHandlerBase<VersionRequest>
    {
        private readonly PeerId _peerId;

        public GetVersionRequestHandler(IObservable<IChanneledMessage<AnySigned>> messageStream,
            IPeerIdentifier peerIdentifier,
            ILogger logger)
            : base(messageStream, logger)
        {
            _peerId = peerIdentifier.PeerId;
        }

        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            if (message == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            Logger.Debug("received message of type VersionRequest");
            try
            {
                var deserialised = message.Payload.FromAnySigned<VersionRequest>();
                Logger.Debug("message content is {0}", deserialised);
                var response = new VersionResponse
                {
                    Version = NodeUtil.GetVersion()
                };

                var anySignedResponse = response.ToAnySigned(_peerId, message.Payload.CorrelationId.ToGuid());
                message.Context.Channel.WriteAndFlushAsync(anySignedResponse).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetVersionRequest after receiving message {0}", message);
                throw;
            }
        }
    }
}
