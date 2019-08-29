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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Registry;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Keystore;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.P2P;
using Catalyst.Common.Registry;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.Shell;
using Catalyst.Common.Types;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Node.Rpc.Client;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Node.Rpc.Client.IO.Transport.Channels;
using Catalyst.Simulator.Interfaces;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Simulator.RpcClients
{
    public class SimpleRpcClient : IRpcClient
    {
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _senderPeerIdentifier;
        private IPeerIdentifier _recipientPeerIdentifier;
        private INodeRpcClient _nodeRpcClient;
        private readonly X509Certificate2 _certificate;
        private readonly NodeRpcClientFactory _nodeRpcClientFactory;

        public SimpleRpcClient(IUserOutput userOutput,
            IPasswordRegistry passwordRegistry,
            X509Certificate2 certificate,
            ILogger logger,
            ISigningContextProvider signingContextProvider)
        {
            _logger = logger;
            _certificate = certificate;

            var fileSystem = new FileSystem();

            var consolePasswordReader = new ConsolePasswordReader(userOutput, new ConsoleUserInput());
            var passwordManager = new PasswordManager(consolePasswordReader, passwordRegistry);

            var wrapper = new CryptoWrapper();
            var cryptoContext = new CryptoContext(wrapper);

            var keyServiceStore = new KeyStoreServiceWrapped(cryptoContext);

            var multiHashAlgorithm = new BLAKE2B_256();
            var addressHelper = new AddressHelper(multiHashAlgorithm);
            var localKeyStore = new LocalKeyStore(passwordManager, cryptoContext, keyServiceStore, fileSystem,
                _logger, addressHelper);

            var keyRegistry = new KeyRegistry();
            var keySigner = new KeySigner(localKeyStore, cryptoContext, keyRegistry);

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);
            var changeTokenProvider = new TtlChangeTokenProvider(10000);
            var messageCorrelationManager = new RpcMessageCorrelationManager(memoryCache, _logger, changeTokenProvider);
            var peerIdValidator = new PeerIdValidator(cryptoContext);
            var nodeRpcClientChannelFactory =
                new NodeRpcClientChannelFactory(keySigner, messageCorrelationManager, peerIdValidator, signingContextProvider);

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 4
            };

            var tcpClientEventLoopGroupFactory = new TcpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);

            var handlers = new List<IRpcResponseObserver>
            {
                new BroadcastRawTransactionResponseObserver(_logger),
                new GetVersionResponseObserver(_logger)
            };

            _nodeRpcClientFactory =
                new NodeRpcClientFactory(nodeRpcClientChannelFactory, tcpClientEventLoopGroupFactory, handlers);

            //PeerIdentifier for RPC/TCP is currently redundant.
            _senderPeerIdentifier =
                new PeerIdentifier(keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).GetPublicKey().Bytes,
                    IPAddress.Any, 1026);
        }

        public async Task<bool> ConnectRetryAsync(IPeerIdentifier peerIdentifier, int retryAttempts = 5)
        {
            var retryCountDown = retryAttempts;
            while (retryCountDown > 0)
            {
                var isConnectionSuccessful = await ConnectAsync(peerIdentifier).ConfigureAwait(false);
                if (isConnectionSuccessful)
                {
                    return true;
                }

                _logger.Error("Connection failed retrying...");
                if (retryAttempts != 0)
                {
                    retryCountDown--;
                }

                await Task.Delay(5000).ConfigureAwait(false);
            }

            return false;
        }

        public async Task<bool> ConnectAsync(IPeerIdentifier peerIdentifier)
        {
            _recipientPeerIdentifier = peerIdentifier;

            var peerRpcConfig = new NodeRpcConfig
            {
                HostAddress = _recipientPeerIdentifier.Ip,
                Port = _recipientPeerIdentifier.Port,
                PublicKey = _recipientPeerIdentifier.PublicKey.KeyToString()
            };

            _logger.Information($"Connecting to {_recipientPeerIdentifier.Ip}:{_recipientPeerIdentifier.Port}");

            try
            {
                _nodeRpcClient =
                    await _nodeRpcClientFactory.GetClient(_certificate, peerRpcConfig).ConfigureAwait(false);
                return _nodeRpcClient.Channel.Open;
            }
            catch (ConnectException connectionException)
            {
                _logger.Error(connectionException, "Could not connect to node");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error attempting to connect to node");
            }

            return false;
        }

        public bool IsConnected() { return _nodeRpcClient.Channel.Active; }

        public void SendMessage<T>(T message) where T : IMessage
        {
            var protocolMessage =
                message.ToProtocolMessage(_senderPeerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            var messageDto = new MessageDto(
                protocolMessage,
                _recipientPeerIdentifier);

            _nodeRpcClient.SendMessage(messageDto);
        }

        public void ReceiveMessage<T>(Action<T> message) where T : IMessage<T>
        {
            _nodeRpcClient.SubscribeToResponse<T>(message.Invoke);
        }
    }
}
