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
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    public abstract class CorrelatableMessageHandlerBase<TProto, TCorrelator>
        : MessageHandlerBase<TProto>
        where TProto : IMessage
        where TCorrelator : IMessageCorrelationCache
    {
        protected TCorrelator _correlationCache;

        protected CorrelatableMessageHandlerBase(TCorrelator correlationCache,
            ILogger logger)
            : base(logger)
        {
            _correlationCache = correlationCache;
        }

        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            Logger.Debug("handle message in correlatable handler");
            try
            {
                Handler(message);
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle CorrelatableMessageHandlerBase after receiving message {0}", message);
                message.Context.Channel.CloseAsync();
            }
        }
    }
}
