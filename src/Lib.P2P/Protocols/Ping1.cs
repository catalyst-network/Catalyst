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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using MultiFormats;
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   Ping Protocol version 1.0
    /// </summary>
    public class Ping1 : IPeerProtocol, IService
    {
        private const int PingSize = 32;

        private static ILog _log = LogManager.GetLogger(typeof(Ping1));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/ping";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1);

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public SwarmService SwarmService { get; set; }

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="swarmService"></param>
        public Ping1(SwarmService swarmService) { SwarmService = swarmService; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            while (true)
            {
                // Read the message.
                var request = new byte[PingSize];
                await stream.ReadExactAsync(request, 0, PingSize, cancel).ConfigureAwait(false);
                _log.Debug($"got ping from {connection.RemotePeer}");

                // Echo the message
                await stream.WriteAsync(request, 0, PingSize, cancel).ConfigureAwait(false);
                await stream.FlushAsync(cancel).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task StartAsync()
        {
            _log.Debug("Starting");

            SwarmService.AddProtocol(this);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            _log.Debug("Stopping");

            SwarmService.RemoveProtocol(this);

            return Task.CompletedTask;
        }

        /// <summary>
        ///   Send echo requests to a peer.
        /// </summary>
        /// <param name="peerId">
        ///   The peer ID to receive the echo requests.
        /// </param>
        /// <param name="count">
        ///   The number of echo requests to send.  Defaults to 10.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the sequence of <see cref="PingResult"/>.
        /// </returns>
        public async Task<IEnumerable<PingResult>> PingAsync(MultiHash peerId,
            int count = 10,
            CancellationToken cancel = default)
        {
            var peer = new Peer {Id = peerId};
            return await PingAsync(peer, count, cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Send echo requests to a peer.
        /// </summary>
        /// <param name="address">
        ///   The address of a peer to receive the echo requests.
        /// </param>
        /// <param name="count">
        ///   The number of echo requests to send.  Defaults to 10.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the sequence of <see cref="PingResult"/>.
        /// </returns>
        public async Task<IEnumerable<PingResult>> PingAsync(MultiAddress address,
            int count = 10,
            CancellationToken cancel = default)
        {
            var peer = SwarmService.RegisterPeerAddress(address);
            return await PingAsync(peer, count, cancel).ConfigureAwait(false);
        }

        private async Task<IEnumerable<PingResult>> PingAsync(Peer peer, int count, CancellationToken cancel)
        {
            var ping = new byte[PingSize];
            var rng = new Random();
            var results = new List<PingResult>
            {
                new PingResult {Success = true, Text = $"PING {peer}."}
            };
            var totalTime = TimeSpan.Zero;

            await using (var stream = await SwarmService.DialAsync(peer, ToString(), cancel))
            {
                for (var i = 0; i < count; ++i)
                {
                    rng.NextBytes(ping);

                    var start = DateTime.Now;
                    try
                    {
                        await stream.WriteAsync(ping, 0, ping.Length, cancel).ConfigureAwait(false);
                        
                        await stream.FlushAsync(cancel).ConfigureAwait(false);

                        var response = new byte[PingSize];
                        await stream.ReadExactAsync(response, 0, PingSize, cancel).ConfigureAwait(false);

                        var result = new PingResult
                        {
                            Time = DateTime.Now - start,
                        };
                        totalTime += result.Time;
                        if (ping.SequenceEqual(response))
                        {
                            result.Success = true;
                            result.Text = "";
                        }
                        else
                        {
                            result.Success = false;
                            result.Text = "ping packet was incorrect!";
                        }

                        results.Add(result);
                    }
                    catch (Exception e)
                    {
                        results.Add(new PingResult
                        {
                            Success = false,
                            Time = DateTime.Now - start,
                            Text = e.Message
                        });
                    }
                }
            }

            var avg = totalTime.TotalMilliseconds / count;
            results.Add(new PingResult
            {
                Success = true,
                Text = $"Average latency: {avg.ToString("0.000")}ms"
            });

            return results;
        }
    }

    internal class PingMessage { }
}
