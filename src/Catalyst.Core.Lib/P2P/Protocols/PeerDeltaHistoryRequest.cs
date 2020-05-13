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
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.P2P.Protocols
{
    public class PeerDeltaHistoryRequest : ProtocolRequestBase, IPeerDeltaHistoryRequest
    {
        public ReplaySubject<IPeerDeltaHistoryResponse> DeltaHistoryResponseMessageStreamer { get; }

        public PeerDeltaHistoryRequest(ILogger logger,
            ILibP2PPeerClient peerClient,
            IPeerSettings peerSettings,
            ICancellationTokenProvider cancellationTokenProvider,
            IScheduler observableScheduler = null) : base(logger, peerSettings.PeerId, cancellationTokenProvider, peerClient)
        {
            DeltaHistoryResponseMessageStreamer = new ReplaySubject<IPeerDeltaHistoryResponse>(1, observableScheduler ?? Scheduler.Default);
        }

        public async Task<IPeerDeltaHistoryResponse> DeltaHistoryAsync(MultiAddress recipientPeerId, uint height = 1, uint range = 1024)
        {
            IPeerDeltaHistoryResponse history;
            try
            {
                PeerClient.SendMessage(new MessageDto(
                    new DeltaHistoryRequest
                    {
                        Range = range,
                        Height = height
                    }.ToProtocolMessage(PeerId, CorrelationId.GenerateCorrelationId()),
                    recipientPeerId
                ));
                
                //todo
                //using (CancellationTokenProvider.CancellationTokenSource)
                //{
                //    history = await DeltaHistoryResponseMessageStreamer
                //       .FirstAsync(a => a != null 
                //         && a.PeerId.PublicKey.SequenceEqual(recipientPeerId.PublicKey) 
                //         && a.PeerId.Ip.SequenceEqual(recipientPeerId.Ip))
                //       .ToTask(CancellationTokenProvider.CancellationTokenSource.Token)
                //       .ConfigureAwait(false);
                //}
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception e)
            {
                Logger.Error(e, nameof(DeltaHistoryAsync));
                return null;
            }

            //return history;
            return null;
        }

        public void Dispose() { Dispose(true); }

        private void Dispose(bool disposing)
        {
            Disposing = disposing;
            if (!Disposing)
            {
                return;
            }

            DeltaHistoryResponseMessageStreamer?.Dispose();
        }
    }
}
