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
using System.Data;
using System.Reactive.Linq;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.IO.Inbound;
using Catalyst.Node.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Messaging
{
    public abstract class AbstractMessageHandlerBase<TProto> : IMessageHandler, IDisposable where TProto : IMessage
    {
        private IDisposable _messageSubscription;
        protected readonly ILogger Logger;

        protected AbstractMessageHandlerBase(ILogger logger)
        {
            Logger = logger;
        }

        public void StartObserving(IObservable<IChanneledMessage<AnySigned>> messageStream)
        {
            if (_messageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }

            var filterMessageType = typeof(TProto).ShortenedProtoFullName();
            _messageSubscription = messageStream
               .Where(m => m != null
                 && m.Payload.TypeUrl == filterMessageType
                 && !m.Equals(NullObjects.ChanneledAnySigned))
               .Subscribe(HandleMessage);
        }

        public virtual void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            Logger.Debug("Pre Handle Message Called");
            Handler(message);
        }

        protected abstract void Handler(IChanneledMessage<AnySigned> message);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _messageSubscription?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
