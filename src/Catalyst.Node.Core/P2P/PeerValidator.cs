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
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Serilog;
using Nethereum.RLP;


namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerValidator : IPeerValidator
    {
        private readonly IPeerClient _peerClient;
        private readonly IReputableCache _reputableCache;
        private readonly IPeerSettings _peerSettings;
        private readonly IP2PService _p2PService;
        private readonly AnySignedMessageObserver _serverObserver;
        private readonly ILogger _logger;

        public PeerValidator(IPeerClient peerClient,
            IReputableCache reputableCache,
            IPeerSettings peerSettings,
            IP2PService p2PService,
            AnySignedMessageObserver serverObserver, 
            ILogger logger)
        {
            _peerClient = peerClient;
            _reputableCache = reputableCache;
            _peerSettings = peerSettings;
            _p2PService = p2PService;
            _serverObserver = serverObserver;
            _logger = logger;
        }

        public bool Validate(PeerId peerId)
        {
            try
            {
                using (_p2PService.MessageStream.Subscribe(_serverObserver))
                {
                    var datagramEnvelope = new P2PMessageFactory(_reputableCache).GetMessageInDatagramEnvelope(
                        new MessageDto(
                            new PingRequest(),
                            MessageTypes.Tell,
                            new PeerIdentifier(peerId),
                            new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(),
                                _peerSettings.BindAddress,
                                _peerSettings.Port)
                        ),
                        Guid.NewGuid()
                    );

                    ((PeerClient) _peerClient).SendMessage(datagramEnvelope).GetAwaiter().GetResult();

                    //there is a Better way of doing all this
                    var tasks = new IChanneledMessageStreamer<AnySigned>[]
                        {
                            _p2PService, _peerClient
                        }
                       .Select(async p =>
                            await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                       .ToArray();

                    Task.WaitAll(tasks, TimeSpan.FromMilliseconds(1000));

                    if (_serverObserver.Received.Payload.PeerId.PublicKey.ToStringUtf8() ==
                        peerId.PublicKey.ToStringUtf8())
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                _p2PService.Dispose();
            }

            return false;
        }

        public async Task<bool> PeerChallengeResponseAsync(PeerIdentifier recipientPeerIdentifier)
        {
            try
            {
                using (_p2PService.MessageStream.Subscribe(_serverObserver))
                {
                    var datagramEnvelope = new P2PMessageFactory(_reputableCache).GetMessageInDatagramEnvelope(
                        new MessageDto(
                            new PingRequest(),
                            MessageTypes.Tell,
                            new PeerIdentifier(recipientPeerIdentifier.PeerId),
                            new PeerIdentifier(_peerSettings.PublicKey.ToBytesForRLPEncoding(),
                                _peerSettings.BindAddress,
                                _peerSettings.Port)
                        ),
                        Guid.NewGuid()
                    );

                    ((PeerClient) _peerClient).SendMessage(datagramEnvelope).GetAwaiter().GetResult();

                    await Task.Delay(TimeSpan.FromMilliseconds(2000));

                    if (_serverObserver.Received.Payload.PeerId.PublicKey.ToStringUtf8() ==
                        recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
                    {
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                _p2PService.Dispose();
            }

            return false;
        }
    }
}
