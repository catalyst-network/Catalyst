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
using Catalyst.Common.IO.Transport;
using Google.Protobuf;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;

namespace Catalyst.Node.Rpc.Client
{
    /// <summary>
    ///     This class provides a command line interface (CLI) application to connect to Catalyst Node.
    ///     Through the CLI the node operator will be able to connect to any number of running nodes and run commands.
    /// </summary>
    internal sealed class NodeRpcClient : TcpClient, INodeRpcClient
    {
        /* Thread safe collection, in the case multiple observers are subscribing on multiple threads at the same time, removes concurrency issues. */
        private readonly ConcurrentBag<IDisposable> _messageSubscriptions;
        private readonly IObservable<IObserverDto<ProtocolMessage>> _socketMessageStream;
        private readonly Dictionary<string, IRpcResponseObserver> _handlers;

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

            var socket = channelFactory.BuildChannel(EventLoopGroupFactory, nodeConfig.HostAddress, nodeConfig.Port, certificate);

            _socketMessageStream = socket.MessageStream;

            _handlers = handlers.ToDictionary(x => x.GetType().BaseType.GenericTypeArguments[0].ShortenedProtoFullName(), x => x);

            Channel = socket.Channel;
        }

        public void SubscribeToResponse<T>(Action<T> onNext) where T : IMessage<T>
        {
            var subscription = _socketMessageStream.Where(x => x.Payload.TypeUrl == typeof(T).ShortenedProtoFullName())
               .Select(x =>
                {
                    var obj = x.Payload.FromProtocolMessage<T>();
                    if (!_handlers.ContainsKey(x.Payload.TypeUrl)) return obj;

                    var handler = _handlers[x.Payload.TypeUrl];
                    handler.HandleResponseObserver(obj, x.Context, new PeerIdentifier(x.Payload.PeerId),
                        x.Payload.CorrelationId.ToCorrelationId());

                    return obj;
                }).Subscribe(onNext);

            _messageSubscriptions.Add(subscription);
        }

        private void DisposeMessageSubscriptions()
        {
            foreach (var subscription in _messageSubscriptions)
            {
                subscription?.Dispose();
            }

            _messageSubscriptions.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            DisposeMessageSubscriptions();
            base.Dispose(disposing);
        }
    }
}
