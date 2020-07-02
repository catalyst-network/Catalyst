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

using Common.Logging;
using MultiFormats;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.Routing
{
    /// <summary>
    ///   A query that is sent to multiple peers.
    /// </summary>
    /// <typeparam name="T">
    ///  The type of answer returned by a peer.
    /// </typeparam>
    public class DistributedQuery<T> where T : class
    {
        private static readonly ILog Log = LogManager.GetLogger("Lib.P2P.Routing.DistributedQuery");
        private static int _nextQueryId = 1;

        /// <summary>
        ///   The maximum number of peers that can be queried at one time
        ///   for all distributed queries.
        /// </summary>
        private static readonly SemaphoreSlim AskCount = new SemaphoreSlim(128);

        /// <summary>
        ///   The maximum time spent on waiting for an answer from a peer.
        /// </summary>
        private static readonly TimeSpan AskTime = TimeSpan.FromSeconds(10);

        /// <summary>
        ///   Controls the running of the distributed query.
        /// </summary>
        /// <remarks>
        ///   Becomes cancelled when the correct number of answers are found
        ///   or the caller of <see cref="RunAsync"/> wants to cancel
        ///   or the DHT is stopped.
        /// </remarks>
        private CancellationTokenSource _runningQuery;

        private readonly ConcurrentDictionary<Peer, Peer> _visited = new ConcurrentDictionary<Peer, Peer>();
        private readonly ConcurrentDictionary<T, T> _answers = new ConcurrentDictionary<T, T>();
        private DhtMessage _queryMessage;
        private int _failedConnects;

        /// <summary>
        ///   Raised when an answer is obtained.
        /// </summary>
        public event EventHandler<T> AnswerObtained;

        /// <summary>
        ///   The unique identifier of the query.
        /// </summary>
        public int Id { get; } = _nextQueryId++;

        /// <summary>
        ///   The received answers for the query.
        /// </summary>
        public IEnumerable<T> Answers => _answers.Values;

        /// <summary>
        ///   The number of answers needed.
        /// </summary>
        /// <remarks>
        ///   When the numbers <see cref="Answers"/> reaches this limit
        ///   the <see cref="RunAsync">running query</see> will stop.
        /// </remarks>
        internal int AnswersNeeded { get; set; } = 1;

        /// <summary>
        ///   The maximum number of concurrent peer queries to perform
        ///   for one distributed query.
        /// </summary>
        /// <value>
        ///   The default is 16.
        /// </value>
        /// <remarks>
        ///   The number of peers that are asked for the answer.
        /// </remarks>
        public int ConcurrencyLevel { get; set; } = 16;

        /// <summary>
        ///   The distributed hash table.
        /// </summary>
        public IDhtService Dht { get; set; }

        /// <summary>
        ///   The type of query to perform.
        /// </summary>
        internal MessageType QueryType { get; set; }

        /// <summary>
        ///   The key to find.
        /// </summary>
        internal MultiHash QueryKey { get; set; }

        /// <summary>
        ///   Starts the distributed query.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task RunAsync(CancellationToken cancel)
        {
            Log.Debug($"Q{Id} run {QueryType} {QueryKey}");

            _runningQuery = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            Dht.Stopped += OnDhtStopped;
            _queryMessage = new DhtMessage
            {
                Type = QueryType,
                Key = QueryKey?.ToArray(),
            };

            var tasks = Enumerable
                .Range(1, ConcurrencyLevel)
                .Select(i => { var id = i; return AskAsync(id); });
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // eat it
            }
            finally
            {
                Dht.Stopped -= OnDhtStopped;
            }
            Log.Debug($"Q{Id} found {_answers.Count} answers, visited {_visited.Count} peers, failed {_failedConnects}");
        }

        private void OnDhtStopped(object sender, EventArgs e)
        {
            Log.Debug($"Q{Id} cancelled because DHT stopped.");
            _runningQuery.Cancel();
        }

        /// <summary>
        ///   Ask the next peer the question.
        /// </summary>
        public async Task AskAsync(int taskId)
        {
            var pass = 0;
            var waits = 20;
            while (!_runningQuery.IsCancellationRequested && waits > 0)
            {
                // Get the nearest peer that has not been visited.
                var peer = Dht.RoutingTable
                    .NearestPeers(QueryKey)
                    .Where(p => !_visited.ContainsKey(p))
                    .FirstOrDefault();
                if (peer == null)
                {
                    --waits;
                    await Task.Delay(100);
                    continue;
                }

                if (!_visited.TryAdd(peer, peer))
                {
                    continue;
                }
                ++pass;

                // Ask the nearest peer.
                await AskCount.WaitAsync(_runningQuery.Token).ConfigureAwait(false);
                var start = DateTime.Now;
                Log.Debug($"Q{Id}.{taskId}.{pass} ask {peer}");
                try
                {
                    using (var timeout = new CancellationTokenSource(AskTime))
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _runningQuery.Token))
                    using (var stream = await Dht.SwarmService.DialAsync(peer, Dht.ToString(), cts.Token).ConfigureAwait(false))
                    {
                        // Send the KAD query and get a response.
                        Serializer.SerializeWithLengthPrefix(stream, _queryMessage, PrefixStyle.Base128);
                        await stream.FlushAsync(cts.Token).ConfigureAwait(false);
                        var response = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cts.Token).ConfigureAwait(false);

                        // Process answer
                        ProcessProviders(response.ProviderPeers);
                        ProcessCloserPeers(response.CloserPeers);
                    }
                    var time = DateTime.Now - start;
                    Log.Debug($"Q{Id}.{taskId}.{pass} ok {peer} ({time.TotalMilliseconds} ms)");
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref _failedConnects);
                    var time = DateTime.Now - start;
                    Log.Warn($"Q{Id}.{taskId}.{pass} failed ({time.TotalMilliseconds} ms) - {e.Message}");
                    // eat it
                }
                finally
                {
                    AskCount.Release();
                }
            }
        }

        private void ProcessProviders(DhtPeerMessage[] providers)
        {
            if (providers == null)
            {
                return;
            }

            foreach (var provider in providers)
            {
                if (provider.TryToPeer(out Peer p))
                {
                    if (p == Dht.SwarmService.LocalPeer || !Dht.SwarmService.IsAllowed(p))
                    {
                        continue;
                    }

                    p = Dht.SwarmService.RegisterPeer(p);
                    if (QueryType == MessageType.GetProviders)
                    {
                        // Only unique answers
                        var answer = p as T;
                        if (!_answers.ContainsKey(answer))
                        {
                            AddAnswer(answer);
                        }
                    }
                }
            }
        }

        private void ProcessCloserPeers(DhtPeerMessage[] closerPeers)
        {
            if (closerPeers == null)
            {
                return;
            }

            foreach (var closer in closerPeers)
            {
                if (closer.TryToPeer(out Peer p))
                {
                    if (p == Dht.SwarmService.LocalPeer || !Dht.SwarmService.IsAllowed(p))
                    {
                        continue;
                    }

                    p = Dht.SwarmService.RegisterPeer(p);
                    if (QueryType == MessageType.FindNode && QueryKey == p.Id)
                    {
                        AddAnswer(p as T);
                    }
                }
            }
        }

        /// <summary>
        ///   Add a answer to the query.
        /// </summary>
        /// <param name="answer">
        ///   An answer.
        /// </param>
        /// <remarks>
        /// </remarks>
        internal void AddAnswer(T answer)
        {
            if (answer == null)
            {
                return;
            }

            if (_runningQuery != null && _runningQuery.IsCancellationRequested)
            {
                return;
            }

            if (_answers.TryAdd(answer, answer))
            {
                if (_answers.Count >= AnswersNeeded && _runningQuery != null && !_runningQuery.IsCancellationRequested)
                {
                    _runningQuery.Cancel(false);
                }
            }

            AnswerObtained?.Invoke(this, answer);
        }
    }
}
