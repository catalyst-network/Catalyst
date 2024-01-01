#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

namespace Lib.P2P.PubSub
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPubSub
    {
        /// <summary>
        ///   Get the subscribed topics.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a sequence of <see cref="string"/> for each topic.
        /// </returns>
        Task<IEnumerable<string>> SubscribedTopicsAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Get the peers that are pubsubing with us.
        /// </summary>
        /// <param name="topic">
        ///   When specified, only peers subscribing on the topic are returned.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a sequence of <see cref="Peer"/>.
        /// </returns>
        Task<IEnumerable<Peer>> PeersAsync(string topic = null, CancellationToken cancel = default);

        /// <summary>
        ///   Publish a string message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, string message, CancellationToken cancel = default);

        /// <summary>
        ///   Publish a binary message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, byte[] message, CancellationToken cancel = default);

        /// <summary>
        ///   Publish a binary message to a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="message">
        ///   The message to publish.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(string topic, Stream message, CancellationToken cancel = default);

        /// <summary>
        ///   Subscribe to messages on a given topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic name.
        /// </param>
        /// <param name="handler">
        ///   The action to perform when a <see cref="IPublishedMessage"/> is received.
        /// </param>
        /// <param name="cancellationToken">
        ///   Is used to stop the topic listener.  When cancelled, the <see cref="OperationCanceledException"/>
        ///   is <b>NOT</b> raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   The <paramref name="handler"/> is invoked on the topic listener thread.
        /// </remarks>
        Task SubscribeAsync(string topic, Action<IPublishedMessage> handler, CancellationToken cancellationToken);
    }
}
