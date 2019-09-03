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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Types;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.IO.Observers
{
    public abstract class BroadcastObserverBase<TProto> : MessageObserverBase, IBroadcastObserver where TProto : IMessage
    {
        protected BroadcastObserverBase(ILogger logger) : base(logger, typeof(TProto).ShortenedProtoFullName())
        {
            Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsBroadcastType(),
                t => $"{nameof(TProto)} is not of type {MessageTypes.Broadcast.Name}");
        }

        public abstract void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto);

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            try
            {
                HandleBroadcast(messageDto);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to handle message");
            }
        }
    }
}
