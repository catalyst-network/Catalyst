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
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Extensions;
using Serilog;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using System.Collections.Concurrent;
using Catalyst.Protocol;

namespace Catalyst.Core.Lib.P2P
{
    public class PeerChallenger : IPeerChallenger, IDisposable
    {
        private readonly IPeerService _peerService;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _senderIdentifier;

        private readonly IDisposable _incomingPingResponseSubscription;
        private readonly ConcurrentStack<IObserverDto<ProtocolMessage>> _receivedResponses;

        private readonly IPeerClient _peerClient;
        private readonly string _messageType = PingResponse.Descriptor.ShortenedFullName();

        public object PeerIdentifierHelper { get; private set; }

        public PeerChallenger(IPeerSettings peerSettings,
            IPeerService peerService,
            ILogger logger,
            IPeerClient peerClient,
            IPeerIdentifier senderIdentifier) 
        {
            _receivedResponses = new ConcurrentStack<IObserverDto<ProtocolMessage>>();
            _incomingPingResponseSubscription = peerService.MessageStream.Subscribe(this);

            _peerService = peerService;
            _senderIdentifier = senderIdentifier;
            _logger = logger;
            _peerClient = peerClient;
        }

        public void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            if (messageDto.Payload.Equals(NullObjects.ProtocolMessage))
            {
                return;
            }

            if (messageDto.Payload.TypeUrl == _messageType)
            {
                _receivedResponses.Push(messageDto);
            }
        }

        public void OnCompleted() { _logger.Information("End of {0} stream.", nameof(ProtocolMessage)); }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(ProtocolMessage));
        }

        public bool ChallengePeer(IPeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                var correlationId = CorrelationId.GenerateCorrelationId();

                var protocolMessage = new PingRequest().ToProtocolMessage(_senderIdentifier.PeerId, correlationId);
                var messageDto = new MessageDto<ProtocolMessage>(
                    protocolMessage,
                    _senderIdentifier,
                    recipientPeerIdentifier,
                  correlationId
                );

                ((PeerClient)_peerClient).SendMessage(messageDto);

                var tasks = new IObservableMessageStreamer<ProtocolMessage>[]
                    {
                        _peerService
                    }
                    .Select(async (p, n) =>                    
                        await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ObserverDto
                        && a.Payload.TypeUrl == _messageType
                        && a.Payload.PeerId.PublicKey.ToStringUtf8() == recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())                        )
                   .ToArray();              

                Task.WaitAny(tasks, TimeSpan.FromMilliseconds(20000));

                if (_receivedResponses.Any() && _receivedResponses.Last().Payload.PeerId.PublicKey.ToStringUtf8() ==
                        recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }

            return false;
        }


        public bool PeerChallengeResponseA(IPeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                var correlationId = CorrelationId.GenerateCorrelationId();

                var protocolMessage = new PingRequest().ToProtocolMessage(_senderIdentifier.PeerId, correlationId);
                var messageDto = new MessageDto<ProtocolMessage>(
                    protocolMessage,
                    _senderIdentifier,
                    recipientPeerIdentifier,
                  correlationId
                );

                ((PeerClient)_peerClient).SendMessage(messageDto);

                var tasks = new IObservableMessageStreamer<ProtocolMessage>[]
                    {
                        _peerService
                    }
                    .Select(async p =>
                        await p.MessageStream.FirstAsync(a => a != null  && a != NullObjects.ObserverDto
                        && a.Payload.TypeUrl == _messageType))
                   .ToArray();

                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(20000));

                if (_receivedResponses.Any() && _receivedResponses.Last().Payload.PeerId.PublicKey.ToStringUtf8() ==
                        recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
                {
                        return true;
                }                
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
