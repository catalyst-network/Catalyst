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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.IPPN;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    public sealed class PeerChallengeRequest : ProtocolRequestBase, IPeerChallengeRequest
    {
        private readonly ILogger _logger;
        private readonly MultiAddress _senderIdentifier;
        private readonly IPeerClient _peerClient;
        private readonly int _ttl;

        public ReplaySubject<IPeerChallengeResponse> ChallengeResponseMessageStreamer { get; }

        public PeerChallengeRequest(ILogger logger,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            int ttl,
            IScheduler scheduler = null)
            : base(logger,
                peerSettings.Address,
                new CancellationTokenProvider(ttl),
                peerClient)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;
            ChallengeResponseMessageStreamer = new ReplaySubject<IPeerChallengeResponse>(1, observableScheduler);
            _senderIdentifier = peerSettings.Address;
            _logger = logger;
            _peerClient = peerClient;
            _ttl = ttl;
        }

        public async Task<bool> ChallengePeerAsync(MultiAddress recipientAddress)
        {
            try
            {
                var correlationId = CorrelationId.GenerateCorrelationId();
                var protocolMessage = new PingRequest().ToProtocolMessage(_senderIdentifier, correlationId);

                _logger.Verbose($"Sending peer challenge request to IP: {recipientAddress}");
                await _peerClient.SendMessageAsync(protocolMessage, recipientAddress).ConfigureAwait(false);

                using (var cancellationTokenSource =
                    new CancellationTokenSource(TimeSpan.FromSeconds(_ttl)))
                {
                    await ChallengeResponseMessageStreamer
                       .FirstAsync(a => a != null && a.Address == recipientAddress)
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
