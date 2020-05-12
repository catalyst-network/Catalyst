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
        private readonly Peer _localPeer;
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerLibP2PServerChannelFactory(
            IBroadcastManager broadcastManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            Peer localPeer,
            IPubSubApi pubSubApi,
        IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _broadcastManager = broadcastManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext { NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolPeer };
            _localPeer = localPeer;
            _pubSubApi = pubSubApi;
            _messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>();
            MessageStream = _messageSubject.AsObservable();
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
                        _messageSubject.OnNext(new ObserverDto(null, innerGossipMessageSigned));
                        return;
                    }

                    _messageSubject.OnNext(new ObserverDto(null, proto));
                }
            }, CancellationToken.None);

            return MessageStream;
        }
    }
}
