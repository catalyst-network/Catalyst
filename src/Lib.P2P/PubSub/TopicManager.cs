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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lib.P2P.PubSub
{
    /// <summary>
    ///   Maintains the sequence of peer's that are interested in a topic.
    /// </summary>
    public class TopicManager
    {
        private static readonly IEnumerable<Peer> Nopeers = Enumerable.Empty<Peer>();

        private ConcurrentDictionary<string, HashSet<Peer>> _topics = new ConcurrentDictionary<string, HashSet<Peer>>();

        /// <summary>
        ///   Get the peers interested in a topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interest or <b>null</b> for all topics.
        /// </param>
        /// <returns>
        ///   A sequence of <see cref="Peer"/> that are interested
        ///   in the <paramref name="topic"/>.
        /// </returns>
        public IEnumerable<Peer> GetPeers(string topic)
        {
            if (topic == null) return _topics.Values.SelectMany(v => v);

            if (!_topics.TryGetValue(topic, out var peers)) return Nopeers;
            return peers;
        }

        /// <summary>
        ///   Gets the topics that a peer is interested in
        /// </summary>
        /// <param name="peer">
        ///   The <see cref="Peer"/>.
        /// </param>
        /// <returns>
        ///   A sequence of topics that the <paramref name="peer"/> is
        ///   interested in.
        /// </returns>
        public IEnumerable<string> GetTopics(Peer peer)
        {
            return _topics
               .Where(kp => kp.Value.Contains(peer))
               .Select(kp => kp.Key);
        }

        /// <summary>
        ///   Indicate that the <see cref="Peer"/> is interested in the
        ///   topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interest.
        /// </param>
        /// <param name="peer">
        ///   A <see cref="Peer"/>
        /// </param>
        /// <remarks>
        ///   Duplicates are ignored.
        /// </remarks>
        public void AddInterest(string topic, Peer peer)
        {
            _topics.AddOrUpdate(
                topic,
                (key) => new HashSet<Peer> {peer},
                (key, peers) =>
                {
                    peers.Add(peer);
                    return peers;
                });
        }

        /// <summary>
        ///   Indicate that the <see cref="Peer"/> is not interested in the
        ///   topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic of interest.
        /// </param>
        /// <param name="peer">
        ///   A <see cref="Peer"/>
        /// </param>
        public void RemoveInterest(string topic, Peer peer)
        {
            _topics.AddOrUpdate(
                topic,
                (key) => new HashSet<Peer>(),
                (key, list) =>
                {
                    list.Remove(peer);
                    return list;
                });
        }

        /// <summary>
        ///   Indicates that the peer is not interested in anything. 
        /// </summary>
        /// <param name="peer">
        ///   The <see cref="Peer"/>.s
        /// </param>
        public void Clear(Peer peer)
        {
            foreach (var topic in _topics.Keys)
            {
                RemoveInterest(topic, peer);
            }
        }

        /// <summary>
        ///   Remove all topics.
        /// </summary>
        public void Clear() { _topics.Clear(); }
    }
}
