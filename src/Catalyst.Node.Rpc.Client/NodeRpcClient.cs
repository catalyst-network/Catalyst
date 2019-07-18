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

using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Catalyst.Common.IO.Transport;
using Google.Protobuf;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Catalyst.Node.Rpc.Client
{
    /// <summary>
    ///     This class provides a command line interface (CLI) application to connect to Catalyst Node.
    ///     Through the CLI the node operator will be able to connect to any number of running nodes and run commands.
    /// </summary>
    internal sealed class NodeRpcClient : TcpClient, INodeRpcClient
    {
        private ConcurrentBag<IDisposable> _messageSubscriptions;
        private ReplaySubject<IRpcClientMessageDto<IMessage>> _messageResponse;
        private IObservable<IRpcClientMessageDto<IMessage>> _messageResponseStream;

        /// <summary>
        ///     Initialize a new instance of RPClient
        /// </summary>
        /// <param name="channelFactory"></param>
        /// <param name="certificate"></param>
        /// <param name="nodeConfig">rpc node config</param>
        /// <param name="handlers"></param>
        /// <param name="clientEventLoopGroupFactory"></param>
        public NodeRpcClient(ITcpClientChannelFactory channelFactory,
            X509Certificate2 certificate,
            IRpcNodeConfig nodeConfig,
            IEnumerable<IRpcResponseObserver> handlers,
            ITcpClientEventLoopGroupFactory clientEventLoopGroupFactory)
            : base(channelFactory, Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType),
                clientEventLoopGroupFactory)
        {
            _messageSubscriptions = new ConcurrentBag<IDisposable>();
            _messageResponse = new ReplaySubject<IRpcClientMessageDto<IMessage>>(1);
            _messageResponseStream = _messageResponse.AsObservable();

            var socket = channelFactory.BuildChannel(EventLoopGroupFactory, nodeConfig.HostAddress, nodeConfig.Port, certificate);
            handlers.ToList().ForEach(handler =>
            {
                _messageResponseStream = _messageResponseStream.Merge(handler.MessageResponseStream);
                handler.StartObserving(socket.MessageStream);
            });
            Channel = socket.Channel;
        }

        public void Subscribe<T>(Action<T> onNext)
        {
            _messageSubscriptions.Add(_messageResponseStream.Where(x => x.Message.GetType() == typeof(T)).SubscribeOn(NewThreadScheduler.Default).Subscribe(rpcClientMessageDto =>
                onNext((T)rpcClientMessageDto.Message)
            ));
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var subscription in _messageSubscriptions)
            {
                subscription?.Dispose();
            }
            _messageSubscriptions.Clear();
            _messageResponse?.Dispose();
            base.Dispose(disposing);
        }
    }
}
