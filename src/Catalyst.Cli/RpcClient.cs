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
using System.Threading.Tasks;
using Catalyst.Cli.Handlers;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels.Sockets;
using ILogger = Serilog.ILogger;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;

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
        private readonly AnyTypeClientHandler _clientHandler;
        public IObservable<IChanneledMessage<Any>> MessageStream { get; }

        private readonly GetInfoResponseHandler _getInfoResponseHandler;
        private readonly GetVersionResponseHandler _getVersionResponseHandler;
        private readonly GetMempoolResponseHandler _getMempoolResponseHandler;
        private readonly SignMessageResponseHandler _signMessageResponseHandler;

        /// <summary>
        /// Intialize a new instance of RPClient by doing the following:
        /// 1- Get the settings from the config file
        /// 2- Create/Read the SSL Certificate
        /// 3- Start the client
        /// </summary>
        /// <param name="logger">a logger instance</param>
        /// <param name="certificateStore">certification store object to create/read the SSL certificate</param>
        public RpcClient(ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _certificateStore = certificateStore;
            _clientHandler = new AnyTypeClientHandler();
            MessageStream = _clientHandler.MessageStream;

            _getInfoResponseHandler = new GetInfoResponseHandler(MessageStream, _logger);
            _getVersionResponseHandler = new GetVersionResponseHandler(MessageStream, _logger);
            _getMempoolResponseHandler = new GetMempoolResponseHandler(MessageStream, _logger);
            _signMessageResponseHandler = new SignMessageResponseHandler(MessageStream, _logger);
        }

        public async Task<ISocketClient> GetClientSocketAsync(IRpcNodeConfig nodeConfig)
        {
            try
            {
                var certificate = _certificateStore.ReadOrCreateCertificateFile(nodeConfig.PfxFileName);

                var handlers = new List<IChannelHandler>
                {
                    new ProtobufVarint32LengthFieldPrepender(),
                    new ProtobufEncoder(),
                    new ProtobufVarint32FrameDecoder(),
                    new ProtobufDecoder(Any.Parser),
                    _clientHandler
                };

                var socketClient = await new TcpClient()
                   .Bootstrap(
                        new OutboundChannelInitializer<ISocketChannel>(channel => { },
                            handlers,
                            nodeConfig.HostAddress,
                            certificate
                        )
                    )
                   .ConnectClient(
                        nodeConfig.HostAddress,
                        nodeConfig.Port
                    );


                _logger.Information("Rpc client starting");
                _logger.Information("Connecting to {0} @ {1}:{2}", nodeConfig.NodeId, nodeConfig.HostAddress,
                    nodeConfig.Port);

                return socketClient;
            }
            catch (System.PlatformNotSupportedException exception)
            {
                _logger.Error(exception, "Invalid SSL certificate.");

                throw;
            }
            catch (ConnectException connectException)
            {
                _logger.Error(connectException, "Connection with the server couldn't be established.");

                throw;
            }
            catch (ConnectTimeoutException timeoutException)
            {
                _logger.Error(timeoutException, "Connection timed out.");

                throw;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Connection with the server couldn't be established.");

                throw;
            }
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

        public IRpcNode ConnectToNode(string nodeId, IRpcNodeConfig nodeConfig)
        {
            IRpcNode connectedNode = null;

            try
            {
                //Connect to the node
                var socket = GetClientSocketAsync(nodeConfig).GetAwaiter().GetResult();

                //if a socket could be opened with the node
                //then create IRpcNode and add it the node to the list of connected nodes
                if (socket != null)
                {
                    connectedNode = new RpcNode(nodeConfig, socket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return connectedNode;
        }

        /*Implementing IDisposable */

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("disposing RpcClient");
                _getInfoResponseHandler.Dispose();
                _getVersionResponseHandler.Dispose();
                _getMempoolResponseHandler.Dispose();
                _signMessageResponseHandler.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
