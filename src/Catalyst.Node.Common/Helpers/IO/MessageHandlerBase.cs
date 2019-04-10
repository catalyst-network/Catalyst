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
using System.Reactive.Linq;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO
{
    public abstract class MessageHandlerBase<T> : IMessageHandler, IDisposable where T : IMessage
    {
        private readonly IDisposable _messageSubscription;
        protected readonly ILogger Logger;

        protected MessageHandlerBase(IObservable<IChanneledMessage<Any>> messageStream, ILogger logger)
        {
            Logger = logger;
            var filterMessageType = typeof(T).ShortenedProtoFullName();
            _messageSubscription = messageStream
               .Where(m => m !=null && m.Payload.TypeUrl == filterMessageType)
               .Subscribe(HandleMessage);
        }

        public abstract void HandleMessage(IChanneledMessage<Any> message);

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
