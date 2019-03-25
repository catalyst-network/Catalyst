/*
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels.Sockets;
using ILogger = Serilog.ILogger;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Core.P2P.Messaging;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;
using Makaretu.Dns;

namespace Catalyst.Cli
{
    /// <summary>
    /// This class provides a command line interface (CLI) application to connect to Catalyst Node.
    /// Through the CLI the node operator will be able to connect to any number of running nodes and run commands. 
    /// </summary>
    public class RpcClient : IRpcClient, IDisposable
    {
        private readonly ILogger _logger;      
        private readonly ICertificateStore _certificateStore;
        private AnyTypeClientHandler _clientHanlder;

        /// <summary>
        /// Intialize a new instance of RPClient by doing the following:
        /// 1- Get the settings from the config file
        /// 2- Create/Read the SSL Certificate
        /// 3- Start the client
        /// </summary>
        /// <param name="settings">an object of ClientSettings which reads the settings from config file section RPCClient</param>
        /// <param name="logger">a logger instance</param>
        /// <param name="certificateStore">certification store object to create/read the SSL certificate</param>
        public RpcClient(ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _certificateStore = certificateStore;
            _clientHanlder = new AnyTypeClientHandler();
            MessageStream = _clientHanlder.MessageStream;
            
            //MessageStream 
        }
        
        public async Task<ISocketClient> GetClientSocketAsync(IRpcNodeConfig nodeConfig)
        {
            var certificate = _certificateStore.ReadOrCreateCertificateFile(nodeConfig.PfxFileName);

            _logger.Information("Rpc client starting");
            _logger.Information("Connecting to {0} @ {1}:{2}", nodeConfig.NodeId, nodeConfig.HostAddress, nodeConfig.Port);
            
            var handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(Any.Parser),
                new RpClientHandler()
            };
            
            var socketClient = await new TcpClient(_logger)
               .Bootstrap(
                    new OutboundChannelInitializer<ISocketChannel>(channel => {},
                        handlers,
                        nodeConfig.HostAddress,
                        certificate
                    )
                )
               .ConnectClient(
                    nodeConfig.HostAddress,
                    nodeConfig.Port
                );
            return socketClient;
        }

        /// <summary>
        /// Sends the message to the RPC Server by writing it asynchronously 
        /// </summary>
        /// <param name="node">RpcNode object which is selected to connect to by the user</param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(IRpcNode node, Any message)
        {
            await node.SocketClient.SendMessage(message);
        }

        public IObservable<ContextAny> MessageStream { get; }

        /*Implementing IDisposable */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("disposing RpcClient");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}