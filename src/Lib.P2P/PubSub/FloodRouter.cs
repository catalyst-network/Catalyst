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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Lib.P2P.Protocols;
using ProtoBuf;
using Semver;

namespace Lib.P2P.PubSub
{
    /// <summary>
    ///   The original flood sub router.
    /// </summary>
    public class FloodRouter : IPeerProtocol, IMessageRouter
    {
        private static ILog _log = LogManager.GetLogger(typeof(FloodRouter));

        private MessageTracker _tracker = new MessageTracker();
        private ConcurrentDictionary<string, string> _localTopics = new ConcurrentDictionary<string, string>();

        /// <summary>
        ///   The topics of interest of other peers.
        /// </summary>
        public TopicManager RemoteTopics { get; set; } = new TopicManager();

        /// <inheritdoc />
        public event EventHandler<PublishedMessage> MessageReceived;

        /// <inheritdoc />
        public string Name { get; } = "floodsub";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1);

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public ISwarmService SwarmService { get; set; }

        public FloodRouter(ISwarmService swarmService)
        {
            SwarmService = swarmService;
        }

        /// <inheritdoc />
        public Task StartAsync()
        {
            _log.Debug("Starting");

            SwarmService.AddProtocol(this);
            SwarmService.ConnectionEstablished += Swarm_ConnectionEstablished;
            SwarmService.PeerDisconnected += Swarm_PeerDisconnected;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            _log.Debug("Stopping");

            SwarmService.ConnectionEstablished -= Swarm_ConnectionEstablished;
            SwarmService.PeerDisconnected -= Swarm_PeerDisconnected;
            SwarmService.RemoveProtocol(this);
            RemoteTopics.Clear();
            _localTopics.Clear();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            while (true)
            {
                var request = await ProtoBufHelper.ReadMessageAsync<PubSubMessage>(stream, cancel)
                   .ConfigureAwait(false);
                
                _log.Debug($"got message from {connection.RemotePeer}");

                if (request.Subscriptions != null)
                {
                    foreach (var sub in request.Subscriptions)
                        ProcessSubscription(sub, connection.RemotePeer);
                }

                if (request.PublishedMessages == null)
                {
                    continue;
                }
                
                foreach (var msg in request.PublishedMessages)
                {
                    _log.Debug($"Message for '{string.Join(", ", msg.Topics)}' fowarded by {connection.RemotePeer}");
                    msg.Forwarder = connection.RemotePeer;
                    MessageReceived?.Invoke(this, msg);
                    await PublishAsync(msg, cancel).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///   Process a subscription request from another peer.
        /// </summary>
        /// <param name="sub">
        ///   The subscription request.
        /// </param>
        /// <param name="remote">
        ///   The remote <see cref="Peer"/>.
        /// </param>
        /// <seealso cref="RemoteTopics"/>
        /// <remarks>
        ///   Maintains the <see cref="RemoteTopics"/>.
        /// </remarks>
        public void ProcessSubscription(Subscription sub, Peer remote)
        {
            if (sub.Subscribe)
            {
                _log.Debug($"Subscribe '{sub.Topic}' by {remote}");
                RemoteTopics.AddInterest(sub.Topic, remote);
            }
            else
            {
                _log.Debug($"Unsubscribe '{sub.Topic}' by {remote}");
                RemoteTopics.RemoveInterest(sub.Topic, remote);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Peer> InterestedPeers(string topic) { return RemoteTopics.GetPeers(topic); }

        /// <inheritdoc />
        public async Task JoinTopicAsync(string topic, CancellationToken cancel)
        {
            _localTopics.TryAdd(topic, topic);
            var msg = new PubSubMessage
            {
                Subscriptions = new[]
                {
                    new Subscription
                    {
                        Topic = topic,
                        Subscribe = true
                    }
                }
            };
            try
            {
                var peers = SwarmService.KnownPeers.Where(p => p.ConnectedAddress != null);
                await SendAsync(msg, peers, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Warn("Join topic failed.", e);
            }
        }

        /// <inheritdoc />
        public async Task LeaveTopicAsync(string topic, CancellationToken cancel)
        {
            _localTopics.TryRemove(topic, out _);
            var msg = new PubSubMessage
            {
                Subscriptions = new[]
                {
                    new Subscription
                    {
                        Topic = topic,
                        Subscribe = false
                    }
                }
            };
            try
            {
                var peers = SwarmService.KnownPeers.Where(p => p.ConnectedAddress != null);
                await SendAsync(msg, peers, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Warn("Leave topic failed.", e);
            }
        }

        /// <inheritdoc />
        public Task PublishAsync(PublishedMessage message, CancellationToken cancel)
        {
            if (_tracker.RecentlySeen(message.MessageId))
            {
                return Task.CompletedTask;
            }

            // Find a set of peers that are interested in the topic(s).
            // Exclude author and sender
            var peers = message.Topics
               .SelectMany(topic => RemoteTopics.GetPeers(topic))
               .Where(peer => peer != message.Sender)
               .Where(peer => peer != message.Forwarder);

            // Forward the message.
            var forward = new PubSubMessage
            {
                PublishedMessages = new[] {message}
            };

            return SendAsync(forward, peers, cancel);
        }

        private Task SendAsync(PubSubMessage msg, IEnumerable<Peer> peers, CancellationToken cancel)
        {
            // Get binary representation
            byte[] bin;
            using (var ms = new MemoryStream())
            {
                Serializer.SerializeWithLengthPrefix(ms, msg, PrefixStyle.Base128);
                bin = ms.ToArray();
            }

            return Task.WhenAll(peers.Select(p => SendAsync(bin, p, cancel)));
        }

        private async Task SendAsync(byte[] message, Peer peer, CancellationToken cancel)
        {
            try
            {
                await using (var stream = await SwarmService.DialAsync(peer, ToString(), cancel).ConfigureAwait(false))
                {
                    await stream.WriteAsync(message, 0, message.Length, cancel).ConfigureAwait(false);
                    await stream.FlushAsync(cancel).ConfigureAwait(false);
                }

                _log.Debug($"sending message to {peer}");
            }
            catch (Exception e)
            {
                _log.Debug($"{peer} refused pubsub message.", e);
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        /// <summary>
        ///   Raised when a connection is established to a remote peer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="connection"></param>
        /// <remarks>
        ///   Sends the hello message to the remote peer.  The message contains
        ///   all topics that are of interest to the local peer.
        /// </remarks>
        private async void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (_localTopics.Count == 0)
            {
                return;
            }

            try
            {
                var hello = new PubSubMessage
                {
                    Subscriptions = _localTopics.Values
                       .Select(topic => new Subscription
                        {
                            Subscribe = true,
                            Topic = topic
                        })
                       .ToArray()
                };
                await SendAsync(hello, new[] {connection.RemotePeer}, CancellationToken.None)
                   .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _log.Warn("Sending hello message failed", e);
            }
        }

        /// <summary>
        ///   Raised when the peer has no more connections.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="peer"></param>
        /// <remarks>
        ///   Removes the <paramref name="peer"/> from the
        ///   <see cref="RemoteTopics"/>.
        /// </remarks>
        private void Swarm_PeerDisconnected(object sender, Peer peer) { RemoteTopics.Clear(peer); }
    }
}
