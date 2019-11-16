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
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Serilog;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    /// <summary>
    ///     @TODO This challenge should really check some signature of the peer id
    ///     But that also duplicates part of the functionality provided by pipeline.
    /// </summary>
    public sealed class PeerChallengeRequest : ProtocolRequestBase, IPeerChallengeRequest, IDisposable
    {
        public ReplaySubject<IPeerChallengeResponse> ChallengeResponseMessageStreamer { get; }

        /**
         *     Protocol to ping a target peer and await a response.
         */
        public PeerChallengeRequest(ILogger logger,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            ICancellationTokenProvider cancellationTokenProvider,
            IScheduler observableScheduler = null) : base(logger, peerSettings.PeerId, cancellationTokenProvider, peerClient)
        {
            ChallengeResponseMessageStreamer = new ReplaySubject<IPeerChallengeResponse>(1, observableScheduler ?? Scheduler.Default);
        }
        
        /**
         * Awaitable wrapper of PingRequest
         */
        public async Task<bool> ChallengePeerAsync(PeerId recipientPeerId)
        {
            try
            {
                Logger.Verbose($"Sending peer challenge request to IP: {recipientPeerId}");
                PeerClient.SendMessage(new MessageDto(
                    new PingRequest().ToProtocolMessage(PeerId, CorrelationId.GenerateCorrelationId()),
                    recipientPeerId
                ));
                
                using (CancellationTokenProvider.CancellationTokenSource)
                {
                    // wait on stream until we get a response when the udp server gets
                    // a valid correlatable ping response message it will end up here
                    // then filter the stream to pk/ip we are interested in, as other
                    // parts of the system maybe generating pings
                    await ChallengeResponseMessageStreamer
                       .FirstAsync(a => a != null 
                         && Enumerable.SequenceEqual(a.PeerId.PublicKey, recipientPeerId.PublicKey) 
                         && Enumerable.SequenceEqual(a.PeerId.Ip, recipientPeerId.Ip))
                       .ToTask(CancellationTokenProvider.CancellationTokenSource.Token)
                       .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e, nameof(ChallengePeerAsync));
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            ChallengeResponseMessageStreamer?.Dispose();
        }
    }
}
