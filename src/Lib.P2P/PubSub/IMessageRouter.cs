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
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.PubSub
{
    /// <summary>
    ///   Routes pub/sub messages to other peers.
    /// </summary>
    public interface IMessageRouter : IService
    {
        /// <summary>
        ///   Sends the message to other peers.
        /// </summary>
        /// <param name="message">
        ///   The message to send.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task PublishAsync(PublishedMessage message, CancellationToken cancel);

        /// <summary>
        ///   Raised when a new message is received.
        /// </summary>
        event EventHandler<PublishedMessage> MessageReceived;

        /// <summary>
        ///   Gets the sequence of peers interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interest or <b>null</b> for all topics.
        /// </param>
        /// <returns>
        ///   A sequence of <see cref="Peer"/> that are subsribed to the
        ///   <paramref name="topic"/>.
        /// </returns>
        IEnumerable<Peer> InterestedPeers(string topic);

        /// <summary>
        ///   Indicates that the local peer is interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interested.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task JoinTopicAsync(string topic, CancellationToken cancel);

        /// <summary>
        ///   Indicates that the local peer is no longer interested in the topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interested.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task LeaveTopicAsync(string topic, CancellationToken cancel);
    }
}
