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
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Extensions;
using Serilog;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Util;
using Catalyst.Protocol;
using System.Threading;
using Catalyst.Common.Config;

namespace Catalyst.Core.Lib.P2P
{
    public sealed class PeerChallenger : IPeerChallenger, IDisposable
    {
        private readonly IPeerService _peerService;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _senderIdentifier;
        private readonly IPeerClient _peerClient;
        private readonly string _messageType = PingResponse.Descriptor.ShortenedFullName();

        private readonly CancellationTokenSource _cancellationTokenSource 
            = new CancellationTokenSource(TimeSpan.FromMilliseconds(Constants.WaitTimeForPeerChallengeMilliseconds));

        public object PeerIdentifierHelper { get; private set; }

        public PeerChallenger(IPeerSettings peerSettings,
            IPeerService peerService,
            ILogger logger,
            IPeerClient peerClient,
            IPeerIdentifier senderIdentifier)
        {
            _peerService = peerService;
            _senderIdentifier = senderIdentifier;
            _logger = logger;
            _peerClient = peerClient;
        }

        async public Task<bool> ChallengePeerAsync(IPeerIdentifier recipientPeerIdentifier)
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

                _peerClient.SendMessage(messageDto);

                var t = _peerService.MessageStream.FirstAsync(a => a != null && a != NullObjects.ObserverDto
                    && a.Payload.TypeUrl == _messageType
                    && a.Payload.PeerId.PublicKey.ToStringUtf8() == recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8());

                await t.RunAsync(_cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
            return true;
        }

        public void Dispose()
        {            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}
