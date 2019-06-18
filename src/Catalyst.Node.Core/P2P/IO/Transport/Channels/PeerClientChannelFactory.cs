using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Core.P2P.IO.Transport.Channels
{
    public class PeerClientChannelFactory : UdpClientChannelFactory
    {
        private readonly IKeySigner _keySigner;
        private readonly IMessageCorrelationManager _correlationManager;

        protected override List<IChannelHandler> Handlers =>
            new List<IChannelHandler>
            {
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtoDatagramDecoderHandler(), new ProtoDatagramEncoderHandler()),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new CorrelationHandler(_correlationManager), new CorrelationHandler(_correlationManager))
            };
        
        public PeerClientChannelFactory(IKeySigner keySigner, IMessageCorrelationManager correlationManager)
        {
            _keySigner = keySigner;
            _correlationManager = correlationManager;
        }
        
        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = BootStrapChannel(handlerEventLoopGroupFactory, targetAddress, targetPort);
            
            var messageStream = channel.Pipeline.Get<IObservableServiceHandler>()?.MessageStream;

            return new ObservableChannel(messageStream
             ?? Observable.Never<IProtocolMessageDto<ProtocolMessage>>(), channel);
        }
    }
}
