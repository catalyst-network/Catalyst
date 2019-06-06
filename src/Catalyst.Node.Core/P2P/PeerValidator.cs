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
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerValidator : IPeerValidator,
        IDisposable
    {
        private readonly IPEndPoint _hostEndPoint;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerService _peerService;
        private readonly ILogger _logger;

        private readonly IDisposable _incomingPingResponseSubscription;
        private readonly ConcurrentStack<IChanneledMessage<AnySigned>> _receivedResponses;

        public PeerValidator(IPEndPoint hostEndPoint,
            IPeerSettings peerSettings,
            IPeerService peerService,
            ILogger logger)
        {
            _peerSettings = peerSettings;
            _peerService = peerService;
            _hostEndPoint = hostEndPoint;

            _logger = logger;
            _receivedResponses = new ConcurrentStack<IChanneledMessage<AnySigned>>();
            _incomingPingResponseSubscription = peerService.MessageStream.Subscribe(this);

        }

        public void OnCompleted() { _logger.Information("End of {0} stream.", nameof(AnySigned)); }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(AnySigned));
        }

        public void OnNext(IChanneledMessage<AnySigned> response)
        {
            if (response == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            _receivedResponses.Push(response);
        }

        public bool PeerChallengeResponse(PeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                var datagramEnvelope = new MessageFactory().GetDatagramMessage(
                    new MessageDto(
                        new PingRequest(),
                        MessageTypes.Ask,
                        new PeerIdentifier(recipientPeerIdentifier.PeerId),
                        new PeerIdentifier(_peerSettings)
                    ),
                    Guid.NewGuid()
                );

                using (var peerClient = new PeerClient(_hostEndPoint))
                {
                    peerClient.SendMessage(datagramEnvelope);
                }

                var tasks = new IChanneledMessageStreamer<AnySigned>[]
                    {
                        _peerService 
                    }
                   .Select(async p =>
                        await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                   .ToArray();

                Task.WaitAll(tasks, TimeSpan.FromMilliseconds(2500));

                if (_receivedResponses.Any())
                {
                    if (_receivedResponses.Last().Payload.PeerId.PublicKey.ToStringUtf8() ==
                        recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
                    {
                        return true;
                    }
                }

                return false;
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
