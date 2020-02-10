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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.PubSub;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class PubSubApi : IPubSubApi
    {
        private readonly IPubSubService _pubSubService;

        public PubSubApi(PubSubService pubSubService)
        {
            _pubSubService = pubSubService;
        }

        public async Task<IEnumerable<Peer>> PeersAsync(string topic = null,
            CancellationToken cancel = default)
        {
            return await _pubSubService.PeersAsync(topic, cancel);
        }

        public async Task PublishAsync(string topic,
            string message,
            CancellationToken cancel = default)
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            byte[] message,
            CancellationToken cancel = default)
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task PublishAsync(string topic,
            Stream message,
            CancellationToken cancel = default)
        {
            await _pubSubService.PublishAsync(topic, message, cancel);
        }

        public async Task SubscribeAsync(string topic,
            Action<IPublishedMessage> handler,
            CancellationToken cancellationToken)
        {
            await _pubSubService.SubscribeAsync(topic, handler, cancellationToken);
        }

        public async Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel =
            default)
        {
            return await _pubSubService.SubscribedTopicsAsync(cancel);
        }
    }
}
