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
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.Rpc.IO.Messaging.Correlation
{
    public sealed class RpcMessageCorrelationManager : MessageCorrelationManagerBase, IRpcMessageCorrelationManager
    {
        private readonly ReplaySubject<ICacheEvictionEvent<ProtocolMessage>> _evictionEvent;

        public IObservable<ICacheEvictionEvent<ProtocolMessage>> EvictionEvents => _evictionEvent.AsObservable();

        public RpcMessageCorrelationManager(IMemoryCache cache,
            ILogger logger,
            IChangeTokenProvider changeTokenProvider)
            : base(cache, logger, changeTokenProvider)
        {
            _evictionEvent = new ReplaySubject<ICacheEvictionEvent<ProtocolMessage>>(0);
        }

        protected override void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            Logger.Debug($"{key} message evicted");
            var message = (CorrelatableMessage<ProtocolMessage>) value;
            _evictionEvent.OnNext(new MessageEvictionEvent<ProtocolMessage>(message));
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _evictionEvent?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
