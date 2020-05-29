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

using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Lib.P2P;
using Lib.P2P.Protocols;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P.IO.Transport.Channels
{
    public class PeerLibP2PServerChannelFactory
    {
        private readonly IScheduler _scheduler;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly SigningContext _signingContext;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _messageSubject;
        private readonly IPubSubApi _pubSubApi;
        private readonly ICatalystProtocol _catalystProtocol;
        private readonly Peer _localPeer;
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerLibP2PServerChannelFactory(
            IBroadcastManager broadcastManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            Peer localPeer,
            IPubSubApi pubSubApi,
            ICatalystProtocol catalystProtocol,
        IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _broadcastManager = broadcastManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext { NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolPeer };
            _localPeer = localPeer;
            _pubSubApi = pubSubApi;
            _catalystProtocol = catalystProtocol;
            _messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>();
            MessageStream = _messageSubject.AsObservable();

            _catalystProtocol.MessageStream.Subscribe(message =>
            {
                var innerGossipMessageSigned = ProtocolMessage.Parser.ParseFrom(message.Value);
                _messageSubject.OnNext(new ObserverDto(null, innerGossipMessageSigned));
            });

            var a = 0;
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public async Task<IObservable<IObserverDto<ProtocolMessage>>> BuildMessageStreamAsync()
        {
            await _pubSubApi.SubscribeAsync("catalyst", msg =>
            {
                if (msg.Sender.Id != _localPeer.Id)
                {
                    var proto = ProtocolMessage.Parser.ParseFrom(msg.DataStream);
                    if (proto.IsBroadCastMessage())
                    {
                        var innerGossipMessageSigned = ProtocolMessage.Parser.ParseFrom(proto.Value);
                        //innerGossipMessageSigned.PeerId = msg.Sender.ConnectedAddress.ToString();
                        _messageSubject.OnNext(new ObserverDto(null, innerGossipMessageSigned));
                        return;
                    }

                    //proto.PeerId = msg.Sender.ConnectedAddress.ToString();
                    _messageSubject.OnNext(new ObserverDto(null, proto));
                }
            }, CancellationToken.None);

            return MessageStream;
        }
    }
}
