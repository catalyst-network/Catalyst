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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public abstract class MessageHandlerBase<TProto> : IMessageHandler, IDisposable where TProto : IMessage
    {
        private IDisposable _messageSubscription;
        protected readonly ILogger Logger;

        protected MessageHandlerBase(ILogger logger)
        {
            Logger = logger;
        }

        public void StartObserving(IObservable<IChanneledMessage<ProtocolMessage>> messageStream)
        {
            if (_messageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }

            var filterMessageType = typeof(TProto).ShortenedProtoFullName();
            _messageSubscription = messageStream
               .Where(m => m != null
                 && m.Payload?.TypeUrl == filterMessageType
                 && !m.Equals(NullObjects.ProtocolMessageDto))
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(HandleMessage, HandleError, HandleCompleted);
        }
        
        public void HandleMessage(IChanneledMessage<ProtocolMessage> message)
        {
            Logger.Debug("Pre Handle Message Called");
            Handler(message);
        }

        public virtual void HandleCompleted()
        {
            Logger.Debug("Message stream ended.");
        }

        public virtual void HandleError(Exception exception)
        {
            Logger.Error(exception, "Failed to process message.");
        }

        protected abstract void Handler(IChanneledMessage<ProtocolMessage> message);

        private void Dispose(bool disposing)
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
