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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Common.Logging;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs
{
    /// <summary>
    ///   Periodically queries the DHT to discover new peers.
    /// </summary>
    /// <remarks>
    ///   A backgroud task is created to query the DHT.  It is designed
    ///   to run often at startup and then less often at time increases.
    /// </remarks>
    public sealed class RandomWalk : IService
    {
        private static ILog log = LogManager.GetLogger(typeof(RandomWalk));
        private CancellationTokenSource _cancel;

        /// <summary>
        ///   The Distributed Hash Table to query.
        /// </summary>
        internal IDhtApi Dht { get; set; }

        /// <summary>
        ///   The time to wait until running the query.
        /// </summary>
        public TimeSpan Delay = TimeSpan.FromSeconds(5);

        /// <summary>
        ///   The time to add to the <see cref="Delay"/>.
        /// </summary>
        public TimeSpan DelayIncrement = TimeSpan.FromSeconds(10);

        /// <summary>
        ///   The maximum <see cref="Delay"/>.
        /// </summary>
        public TimeSpan DelayMax = TimeSpan.FromMinutes(9);

        /// <summary>
        ///   Start a background process that will run a random
        ///   walk every <see cref="Delay"/>.
        /// </summary>
        public Task StartAsync()
        {
            if (_cancel != null)
            {
                throw new Exception("Already started.");
            }

            _cancel = new CancellationTokenSource();
            _ = RunnerAsync(_cancel.Token);

            log.Debug("started");
            return Task.CompletedTask;
        }

        /// <summary>
        ///   Stop the background process.
        /// </summary>
        public Task StopAsync()
        {
            _cancel?.Cancel();
            _cancel?.Dispose();
            _cancel = null;

            log.Debug("stopped");
            return Task.CompletedTask;
        }

        /// <summary>
        ///   The background process.
        /// </summary>
        private async Task RunnerAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(Delay, cancellation);
                    await RunQueryAsync(cancellation).ConfigureAwait(false);
                    log.Debug("query finished");
                    Delay += DelayIncrement;
                    if (Delay > DelayMax)
                    {
                        Delay = DelayMax;
                    }
                }
                catch (TaskCanceledException)
                {
                    // eat it.
                }
                catch (Exception e)
                {
                    log.Error("run query failed", e);

                    // eat all exceptions
                }
            }
        }

        private async Task RunQueryAsync(CancellationToken cancel = default)
        {
            // Tests may not set a DHT.
            if (Dht == null)
            {
                return;
            }

            log.Debug("Running a query");

            // Get a random peer id.
            var x = new byte[32];
            var rng = new Random();
            rng.NextBytes(x);
            var id = MultiHash.ComputeHash(x);

            await Dht.FindPeerAsync(id, cancel).ConfigureAwait(false);
        }
    }
}
