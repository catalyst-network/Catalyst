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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Cli;
using Catalyst.Core.Lib.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.FileSystem;
using Catalyst.Core.Lib.IO.EventLoop;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.KeySigner;
using Catalyst.Core.Modules.Keystore;
using Catalyst.Core.Modules.Rpc.Client;
using Catalyst.Core.Modules.Rpc.Client.IO.Observers;
using Catalyst.Core.Modules.Rpc.Client.IO.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Peer;
using Catalyst.Simulator.Interfaces;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Simulator.RpcClients
{
    public class SimpleRpcClient : IRpcClient
    {
        private readonly ILogger _logger;
        private readonly PeerId _senderPeerId;
        private PeerId _recipientPeerId;
        private Abstractions.Rpc.IRpcClient _rpcClient;
        private readonly X509Certificate2 _certificate;
        private readonly RpcClientFactory _rpcClientFactory;

        public SimpleRpcClient(IUserOutput userOutput,
            IPasswordRegistry passwordRegistry,
            X509Certificate2 certificate,
            ILogger logger,
            SigningContext signingContextProvider)
        {
            _logger = logger;
            _certificate = certificate;

            var fileSystem = new FileSystem();

            var consolePasswordReader = new ConsolePasswordReader(userOutput, new ConsoleUserInput());
            var passwordManager = new PasswordManager(consolePasswordReader, passwordRegistry);

            var cryptoContext = new FfiWrapper();

            var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.NetworkType.Returns(signingContextProvider.NetworkType);

            var localKeyStore = new LocalKeyStore(passwordManager, cryptoContext, fileSystem, hashProvider, _logger);

            var keyRegistry = new KeyRegistry();
            var keySigner = new KeySigner(localKeyStore, cryptoContext, keyRegistry);

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);
            var changeTokenProvider = new TtlChangeTokenProvider(10000);
            var messageCorrelationManager = new RpcMessageCorrelationManager(memoryCache, _logger, changeTokenProvider);
            var peerIdValidator = new PeerIdValidator(cryptoContext);

            var nodeRpcClientChannelFactory =
                new RpcClientChannelFactory(keySigner, messageCorrelationManager, peerIdValidator, peerSettings);

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

            _rpcClientFactory =
                new RpcClientFactory(nodeRpcClientChannelFactory, tcpClientEventLoopGroupFactory, handlers);

            //PeerId for RPC/TCP is currently redundant.
            var publicKey = keyRegistry.GetItemFromRegistry(KeyRegistryTypes.DefaultKey).GetPublicKey().Bytes;
            _senderPeerId = publicKey.BuildPeerIdFromPublicKey(IPAddress.Any, 1026);
        }

        public async Task<bool> ConnectRetryAsync(PeerId peerIdentifier, int retryAttempts = 5)
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

        public async Task<bool> ConnectAsync(PeerId peerIdentifier)
        {
            _recipientPeerId = peerIdentifier;

            var peerRpcConfig = new RpcClientSettings
            {
                HostAddress = _recipientPeerId.IpAddress,
                Port = (int) _recipientPeerId.Port,
                PublicKey = _recipientPeerId.PublicKey.KeyToString()
            };

            _logger.Information($"Connecting to {peerRpcConfig.HostAddress}:{peerRpcConfig.Port}");

            try
            {
                _rpcClient =
                    await _rpcClientFactory.GetClientAsync(_certificate, peerRpcConfig).ConfigureAwait(false);
                return _rpcClient.Channel.Open;
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

        public bool IsConnected() { return _rpcClient.Channel.Active; }

        public void SendMessage<T>(T message) where T : IMessage
        {
            var protocolMessage =
                message.ToProtocolMessage(_senderPeerId, CorrelationId.GenerateCorrelationId());
            var messageDto = new MessageDto(
                protocolMessage,
                _recipientPeerId);

            _rpcClient.SendMessage(messageDto);
        }

        public void ReceiveMessage<T>(Action<T> message) where T : IMessage<T>
        {
            _rpcClient.SubscribeToResponse<T>(message.Invoke);
        }
    }
}
