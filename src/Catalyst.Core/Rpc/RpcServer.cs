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
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.IO.Transport;
using Catalyst.Protocol.Common;
using Serilog;

namespace Catalyst.Core.Rpc
{
    public sealed class RpcServer : TcpServer, IRpcServer
    {
        private readonly IEnumerable<IRpcRequestObserver> _requestHandlers;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;

        public IRpcServerSettings Settings { get; }
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; private set; }

        public RpcServer(IRpcServerSettings settings,
            ILogger logger,
            ITcpServerChannelFactory channelFactory,
            ICertificateStore certificateStore,
            IEnumerable<IRpcRequestObserver> requestHandlers,
            ITcpServerEventLoopGroupFactory eventEventLoopGroupFactory) 
            : base(channelFactory, logger, eventEventLoopGroupFactory)
        {
            _requestHandlers = requestHandlers;
            Settings = settings;
            _cancellationSource = new CancellationTokenSource();
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
        }

        public override async Task StartAsync()
        {
            var observableSocket = await ChannelFactory.BuildChannel(EventLoopGroupFactory, Settings.BindAddress, Settings.Port, _certificate);
            Channel = observableSocket.Channel;
            MessageStream = observableSocket.MessageStream;
            _requestHandlers.ToList().ForEach(h => h.StartObserving(MessageStream));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource?.Dispose();
                _certificate?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
