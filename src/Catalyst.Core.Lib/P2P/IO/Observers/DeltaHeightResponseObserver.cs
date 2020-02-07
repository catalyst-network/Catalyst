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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Lib.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using DotNetty.Transport.Channels;
using Lib.P2P;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class DeltaHeightResponseObserver
        : ResponseObserverBase<LatestDeltaHashResponse>,
            IP2PMessageObserver, IPeerClientObservable
    {
        private readonly IPeerQueryTipRequest _peerQueryTipRequest;
        public ReplaySubject<IPeerClientMessageDto> ResponseMessageSubject { get; }
        public IObservable<IPeerClientMessageDto> MessageStream => ResponseMessageSubject.AsObservable();

        public DeltaHeightResponseObserver(ILogger logger, IPeerQueryTipRequest peerQueryTipRequest)
            : base(logger)
        {
            _peerQueryTipRequest = peerQueryTipRequest;
            ResponseMessageSubject = new ReplaySubject<IPeerClientMessageDto>(1);
        }

        protected override void HandleResponse(LatestDeltaHashResponse deltaHeightResponse,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            ResponseMessageSubject.OnNext(new PeerClientMessageDto(deltaHeightResponse, senderPeerId, correlationId));

            _peerQueryTipRequest.QueryTipResponseMessageStreamer.OnNext(
                new PeerQueryTipResponse(senderPeerId, Cid.Read(deltaHeightResponse.Result.Cid.ToByteArray()))
            );
        }
    }
}
