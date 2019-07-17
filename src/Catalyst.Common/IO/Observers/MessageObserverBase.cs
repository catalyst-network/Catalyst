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

using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Catalyst.Common.IO.Observers
{
    public abstract class MessageObserverBase : IMessageObserver, IDisposable
    {
        protected readonly ILogger Logger;
        protected IDisposable MessageSubscription;
        private readonly string _filterMessageType;

        public IChannelHandlerContext ChannelHandlerContext { get; protected set; }

        protected MessageObserverBase(ILogger logger, string filterMessageType)
        {
            Logger = logger;
            _filterMessageType = filterMessageType;
        }

        public void StartObserving(IObservable<IObserverDto<ProtocolMessage>> messageStream)
        {
            if (MessageSubscription != null)
            {
                return;
            }

            MessageSubscription = messageStream
               .Where(m => m.Payload?.TypeUrl != null
                 && m.Payload.TypeUrl == _filterMessageType)
               .SubscribeOn(NewThreadScheduler.Default)
               .Subscribe(this);
        }

        public abstract void OnNext(IObserverDto<ProtocolMessage> messageDto);

        public virtual void OnCompleted()
        {
            Logger.Debug("Message stream ended.");
        }

        public virtual void OnError(Exception exception)
        {

            Logger.Error(exception, "Failed to process message.");
            ChannelHandlerContext?.CloseAsync().ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            MessageSubscription?.Dispose();
            ChannelHandlerContext?.CloseAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
