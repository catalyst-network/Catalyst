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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using System.Net;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observers;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class PeerValidator : ResponseObserverBase<PingResponse>,
        IP2PMessageObserver, 
        IPeerValidator
    {
        private readonly IPEndPoint _hostEndPoint;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerService _peerService;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _senderIdentifier;

        private readonly IDisposable _incomingPingResponseSubscription;

        private readonly IPeerClient _peerClient;

        public object PeerIdentifierHelper { get; private set; }

        public PeerValidator(IPEndPoint hostEndPoint,
            IPeerSettings peerSettings,
            IPeerService peerService,
            ILogger logger,
            IPeerClient peerClient,
            IPeerIdentifier senderIdentifier) : base(logger)
        {
            _peerSettings = peerSettings;
            _peerService = peerService;
            _hostEndPoint = hostEndPoint;
            _senderIdentifier = senderIdentifier;
            _logger = logger;
            _peerClient = peerClient;
        }

        protected override void HandleResponse(PingResponse pingResponse, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId)
        {
            Logger.Debug("received ping response");
        }

        public void OnCompleted() { _logger.Information("End of {0} stream.", nameof(ProtocolMessage)); }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(ProtocolMessage));
        }

        public bool PeerChallengeResponse(PeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                var pingRequest = new PingRequest();
                var messageDto = new MessageDto<PingRequest>(pingRequest,
                    _senderIdentifier,
                    recipientPeerIdentifier,
                    CorrelationId.GenerateCorrelationId());

                ((PeerClient)_peerClient).SendMessage(messageDto);

                //var tasks = new IChanneledMessageStreamer<ProtocolMessage>[]
                //    {
                //        _peerService
                //    }
                //   .Select(async p =>
                //        await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ProtocolMessageDto))
                //   .ToArray();

                //Task.WaitAll(tasks, TimeSpan.FromMilliseconds(2500));

                //if (_receivedResponses.Any())
                //{
                //    if (_receivedResponses.Last().Payload.PeerId.PublicKey.ToStringUtf8() ==
                //        recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
                //    {
                //        return true;
                //    }
                //}

                //return false;
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }

            return false;
        }

        public void Dispose()
        {
            _incomingPingResponseSubscription?.Dispose();
        }
    }
}
