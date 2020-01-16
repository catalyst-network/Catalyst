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

using ProtoBuf;

namespace Lib.P2P.PubSub
{
    /// <summary>
    ///   The PubSub message exchanged between peers.
    /// </summary>
    /// <seealso ref="https://github.com/libp2p/specs/blob/master/pubsub/README.md"/>
    [ProtoContract]
    public class PubSubMessage
    {
        /// <summary>
        ///   Sequence of topic subscriptions of the sender.
        /// </summary>
        [ProtoMember(1)]
        public Subscription[] Subscriptions;

        /// <summary>
        ///   Sequence of topic messages.
        /// </summary>
        [ProtoMember(2)]
        public PublishedMessage[] PublishedMessages;
    }

    /// <summary>
    ///   A peer's subscription to a topic.
    /// </summary>
    /// <seealso ref="https://github.com/libp2p/specs/blob/master/pubsub/README.md"/>
    [ProtoContract]
    public class Subscription
    {
        /// <summary>
        ///   Determines if the topic is subscribed to.
        /// </summary>
        /// <value>
        ///   <b>true</b> if subscribing; otherwise, <b>false</b> if
        ///   unsubscribing.
        /// </value>
        [ProtoMember(1)]
        public bool Subscribe;

        /// <summary>
        ///   The topic name/id.
        /// </summary>
        [ProtoMember(2)]
        public string Topic;
    }
}
