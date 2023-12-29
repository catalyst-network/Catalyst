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
    public sealed class PeerQueryTipRequestRequest : ProtocolRequestBase, IPeerQueryTipRequest
    {
        public ReplaySubject<IPeerQueryTipResponse> QueryTipResponseMessageStreamer { get; }

        /// <summary>
        ///     Protocol for querying a peers current delta height.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="peerClient"></param>
        /// <param name="peerSettings"></param>
        /// <param name="cancellationTokenProvider"></param>
        /// <param name="scheduler"></param>
        public PeerQueryTipRequestRequest(ILogger logger,
            IPeerClient peerClient,
            IPeerSettings peerSettings,
            ICancellationTokenProvider cancellationTokenProvider,
            IScheduler scheduler = null) : base(logger, peerSettings.PeerId, cancellationTokenProvider, peerClient)
        {
            QueryTipResponseMessageStreamer = new ReplaySubject<IPeerQueryTipResponse>(1, scheduler ?? Scheduler.Default);
        }

        public async Task<bool> QueryPeerTipAsync(PeerId recipientPeerId)
        {
            try
            {
                PeerClient.SendMessage(new MessageDto(
                    new LatestDeltaHashRequest().ToProtocolMessage(PeerId, CorrelationId.GenerateCorrelationId()),
                    recipientPeerId
                ));
                
                using (CancellationTokenProvider.CancellationTokenSource)
                {
                    await QueryTipResponseMessageStreamer
                       .FirstAsync(a => a != null 
                         && a.PeerId.PublicKey.SequenceEqual(recipientPeerId.PublicKey) 
                         && a.PeerId.Ip.SequenceEqual(recipientPeerId.Ip))
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
                Logger.Error(e, nameof(QueryPeerTipAsync));
                return false;
            }

            return true;
        }
        
        public void Dispose() { Dispose(true); }
        
        private void Dispose(bool disposing)
        {
            Disposing = disposing;
            if (!Disposing)
            {
                return;
            }

            QueryTipResponseMessageStreamer?.Dispose();
        }
    }
}
