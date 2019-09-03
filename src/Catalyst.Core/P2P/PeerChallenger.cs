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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Serilog;

namespace Catalyst.Core.P2P
{
    public sealed class PeerChallenger : IPeerChallenger, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly IPeerClient _peerClient;
        private readonly int _peerChallengeWaitTimeSeconds;

        public ReplaySubject<IPeerChallengeResponse> ChallengeResponseMessageStreamer { get; }

        public PeerChallenger(ILogger logger,
            IPeerClient peerClient,
            IPeerIdentifier senderIdentifier,
            int peerChallengeWaitTimeSeconds,
            IScheduler scheduler = null)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;
            ChallengeResponseMessageStreamer = new ReplaySubject<IPeerChallengeResponse>(1, observableScheduler);
            _senderIdentifier = senderIdentifier;
            _logger = logger;
            _peerClient = peerClient;
            _peerChallengeWaitTimeSeconds = peerChallengeWaitTimeSeconds;
        }

        public async Task<bool> ChallengePeerAsync(IPeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                var correlationId = CorrelationId.GenerateCorrelationId();
                var protocolMessage = new PingRequest().ToProtocolMessage(_senderIdentifier.PeerId, correlationId);
                var messageDto = new MessageDto(
                    protocolMessage,
                    recipientPeerIdentifier
                );

                _logger.Verbose($"Sending peer challenge request to IP: {recipientPeerIdentifier}");
                _peerClient.SendMessage(messageDto);
                using (var cancellationTokenSource =
                    new CancellationTokenSource(TimeSpan.FromSeconds(_peerChallengeWaitTimeSeconds)))
                {
                    await ChallengeResponseMessageStreamer
                       .FirstAsync(a => a != null 
                         && a.PeerId.PublicKey.SequenceEqual(recipientPeerIdentifier.PeerId.PublicKey) 
                         && a.PeerId.Ip.SequenceEqual(recipientPeerIdentifier.PeerId.Ip))
                       .ToTask(cancellationTokenSource.Token)
                       .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, nameof(ChallengePeerAsync));
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
