#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.PubSub
{
    /// <summary>
    ///   A message router that always raises <see cref="MessageReceived"/>
    ///   when a message is published.
    /// </summary>
    /// <remarks>
    ///   The allows the <see cref="PubSubService"/> to invoke the
    ///   local subscribtion handlers.
    /// </remarks>
    public class LoopbackRouter : IMessageRouter
    {
        private MessageTracker _tracker = new();

        /// <inheritdoc />
        public event EventHandler<PublishedMessage> MessageReceived;

        /// <inheritdoc />
        public IEnumerable<Peer> InterestedPeers(string topic) { return Enumerable.Empty<Peer>(); }

        /// <inheritdoc />
        public Task JoinTopicAsync(string topic, CancellationToken cancel) { return Task.CompletedTask; }

        /// <inheritdoc />
        public Task LeaveTopicAsync(string topic, CancellationToken cancel) { return Task.CompletedTask; }

        /// <inheritdoc />
        public Task PublishAsync(PublishedMessage message, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();

            if (!_tracker.RecentlySeen(message.MessageId)) MessageReceived?.Invoke(this, message);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StartAsync() { return Task.CompletedTask; }

        /// <inheritdoc />
        public Task StopAsync() { return Task.CompletedTask; }
    }
}
