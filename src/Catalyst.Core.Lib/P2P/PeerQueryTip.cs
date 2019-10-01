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
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Serilog;

namespace Catalyst.Core.Lib.P2P
{
    public sealed class PeerQueryTip : IPeerQueryTip, IDisposable
    {
        private readonly ILogger _logger;
        private readonly PeerId _senderIdentifier;
        private readonly IPeerClient _peerClient;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;

        public ReplaySubject<IPeerQueryTipResponse> QueryTipResponseMessageStreamer { get; }

        public PeerQueryTip(ILogger logger,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            ICancellationTokenProvider cancellationTokenProvider,
            IScheduler scheduler = null)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;
            QueryTipResponseMessageStreamer = new ReplaySubject<IPeerQueryTipResponse>(1, observableScheduler);
            _senderIdentifier = peerSettings.PeerId;
            _logger = logger;
            _peerClient = peerClient;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public async Task<bool> QueryPeerTipAsync(PeerId recipientPeerId)
        {
            try
            {
                var messageDto = new MessageDto(
                    new DeltaHeightRequest().ToProtocolMessage(_senderIdentifier, CorrelationId.GenerateCorrelationId()),
                    recipientPeerId
                );
                
                _logger.Verbose($"Query Peer Chain tip to: {recipientPeerId}");

                _peerClient.SendMessage(messageDto);
                
                using (_cancellationTokenProvider.CancellationTokenSource)
                {
                    await QueryTipResponseMessageStreamer
                       .FirstAsync(a => a != null 
                         && a.PeerId.PublicKey.SequenceEqual(recipientPeerId.PublicKey) 
                         && a.PeerId.Ip.SequenceEqual(recipientPeerId.Ip))
                       .ToTask(_cancellationTokenProvider.CancellationTokenSource.Token)
                       .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                _logger.Error(e, nameof(QueryPeerTipAsync));
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            QueryTipResponseMessageStreamer?.Dispose();
        }
    }
}
