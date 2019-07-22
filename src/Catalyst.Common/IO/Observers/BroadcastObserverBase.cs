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

using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Serilog;
using System;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;

namespace Catalyst.Common.IO.Observers
{
    public abstract class BroadcastObserverBase<TProto> : MessageObserverBase, IBroadcastObserver where TProto : IMessage
    {
        private readonly IBroadcastManager _broadcastManager;

        protected BroadcastObserverBase(ILogger logger, IBroadcastManager broadcastManager) : base(logger, typeof(TProto).ShortenedProtoFullName())
        {
            Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsBroadcastType(),
                t => $"{nameof(TProto)} is not of type {MessageTypes.Broadcast.Name}");
            _broadcastManager = broadcastManager;
        }

        public abstract void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto);

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            try
            {
                HandleBroadcast(messageDto);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to handle message");
            }
            finally
            {
                _broadcastManager.RemoveSignedBroadcastMessageData(messageDto.Payload.CorrelationId.ToCorrelationId());
            }
        }
    }
}
