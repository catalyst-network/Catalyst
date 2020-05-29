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
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Wire;
using Common.Logging;
using Google.Protobuf;
using Microsoft.VisualBasic;
using MultiFormats;
using ProtoBuf;
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   Catalyst Protocol version 1.0
    /// </summary>
    public class CatalystProtocol : ICatalystProtocol, IPeerProtocol, IService
    {
        private static ILog _log = LogManager.GetLogger(typeof(CatalystProtocol));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/catalyst";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1);

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public ISwarmService SwarmService { get; set; }

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        public ReplaySubject<ProtocolMessage> ResponseMessageSubject { get; }
        public IObservable<ProtocolMessage> MessageStream { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="swarmService"></param>
        public CatalystProtocol(ISwarmService swarmService)
        {
            SwarmService = swarmService;
            ResponseMessageSubject = new ReplaySubject<ProtocolMessage>(1);
            MessageStream = ResponseMessageSubject.AsObservable();
        }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            await connection.IdentityEstablished.Task.ConfigureAwait(false);

            while (true)
            {
                var intSize = sizeof(int) - 1;
                var length = await stream.ReadVarint32Async(cancel).ConfigureAwait(false);
                var realLength = length - intSize;
                var bytes = new byte[length];
                await stream.ReadExactAsync(bytes, 0, length, cancel).ConfigureAwait(false);

                var newArray = new byte[realLength];
                Array.Copy(bytes, intSize, newArray, 0, realLength);

                var protocolMessage = ProtocolMessage.Parser.ParseFrom(newArray);

                ResponseMessageSubject.OnNext(protocolMessage);
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
        ///   Send a catalyst message request to a peer.
        /// </summary>
        /// <param name="peerId">
        ///   The peer ID to receive the message requests.
        /// </param>
        /// <param name="message">
        ///   The message request to send.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task SendAsync(MultiHash peerId,
            ProtocolMessage message,
            CancellationToken cancel = default)
        {
            var peer = new Peer { Id = peerId };
            await SendAsync(peer, message, cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Send a catalyst message request to a peer.
        /// </summary>
        /// <param name="address">
        ///   The address of a peer to receive the echo requests.
        /// </param>
        /// <param name="message">
        ///   The message request to send.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task SendAsync(MultiAddress address,
            ProtocolMessage message,
            CancellationToken cancel = default)
        {
            var peer = SwarmService.RegisterPeerAddress(address);
            await SendAsync(peer, message, cancel).ConfigureAwait(false);
        }

        private async Task SendAsync(Peer peer, ProtocolMessage message, CancellationToken cancel)
        {
            await using (var stream = await SwarmService.DialAsync(peer, ToString(), cancel))
            {
                var b = message.ToByteArray();
                Serializer.SerializeWithLengthPrefix(stream, message.ToByteArray(), PrefixStyle.Base128);
                await stream.FlushAsync(cancel).ConfigureAwait(false);
            }
        }
    }
}
