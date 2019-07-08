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
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using System.Collections.Concurrent;
using System.Net;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observers;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public class PeerValidator<TProto> : MessageObserverBase,
        IPeerValidator, IP2PMessageObserver, IRpcResponseObserver, IRpcRequestObserver
        where TProto : IMessage, IMessage<TProto>
    {
        private readonly IPEndPoint _hostEndPoint;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerService _peerService;
        private readonly ILogger _logger;

        private readonly IDisposable _incomingPingResponseSubscription;
        //private readonly ConcurrentStack<IChanneledMessage<ProtocolMessage>> _receivedResponses;

        private readonly IPeerClient _peerClient;

        public PeerValidator(IPEndPoint hostEndPoint,
            IPeerSettings peerSettings,
            IPeerService peerService,
            ILogger logger,
            IPeerClient peerClient) : base(logger)
        {
            _peerSettings = peerSettings;
            _peerService = peerService;
            _hostEndPoint = hostEndPoint;

            _logger = logger;

            //_receivedResponses = new ConcurrentStack<IChanneledMessage<ProtocolMessage>>();

            _incomingPingResponseSubscription = peerService.MessageStream.Subscribe(this);

            _peerClient = peerClient;
        }

        public override void StartObserving(IObservable<IObserverDto<ProtocolMessage>> messageStream)
        {
            //if (MessageSubscription != null)
            //{
            //    throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            //}

            //MessageSubscription = messageStream
            //   .Where(m => m.Payload?.TypeUrl != null
            //     && m.Payload.TypeUrl == _filterMessageType)
            //   .SubscribeOn(TaskPoolScheduler.Default)
            //   .Subscribe(OnNext, OnError, OnCompleted);
        }

        public void OnCompleted() { _logger.Information("End of {0} stream.", nameof(ProtocolMessage)); }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(ProtocolMessage));
        }

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            if (messageDto.Payload.Equals(NullObjects.ProtocolMessage)) 
            {
                return;
            }

            //_receivedResponses.Push(messageDto);
        }

        public bool PeerChallengeResponse(PeerIdentifier recipientPeerIdentifier)
        {
            //try
            //{
            //    var datagramEnvelope = new MessageFactory().GetDatagramMessage(
            //        new MessageDto<>(
            //            new PingRequest(),
            //            MessageTypes.Request,
            //            new PeerIdentifier(recipientPeerIdentifier.PeerId),
            //            new PeerIdentifier(_peerSettings)
            //        ),
            //        Guid.NewGuid()
            //    );

            //    ((PeerClient)_peerClient).SendMessageAsync(datagramEnvelope);

            //    var tasks = new IChanneledMessageStreamer<ProtocolMessage>[]
            //        {
            //            _peerService
            //        }
            //       .Select(async p =>
            //            await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ProtocolMessageDto))
            //       .ToArray();

            //    Task.WaitAll(tasks, TimeSpan.FromMilliseconds(2500));

            //    if (_receivedResponses.Any())
            //    {
            //        if (_receivedResponses.Last().Payload.PeerId.PublicKey.ToStringUtf8() ==
            //            recipientPeerIdentifier.PeerId.PublicKey.ToStringUtf8())
            //        {
            //            return true;
            //        }
            //    }

            //    return false;
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e.Message);
            //}

            return false;
        }

        public void Dispose()
        {
            _incomingPingResponseSubscription?.Dispose();
        }
    }
}
