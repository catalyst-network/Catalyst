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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileSystem;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.EventLoop;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Keystore;
using Catalyst.Common.Modules.KeySigner;
using Catalyst.Common.P2P;
using Catalyst.Common.Registry;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.Shell;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Node.Rpc.Client;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Node.Rpc.Client.IO.Transport.Channels;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Simulator
{
    public class NodeSocketInfo
    {
        public INodeRpcClient NodeRpcClient { set; get; }
        public IPeerIdentifier PeerIdentifier { set; get; }
    }

    public class Simulator
    {
        private readonly ILogger _logger;
        private readonly Random _random;
        private readonly KeySigner _keySigner;
        private readonly NodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ConsoleUserOutput _userOutput;
        private readonly X509Certificate2 _certificate;

        public Simulator(PasswordRegistry passwordRegistry)
        {
            _random = new Random();
            _logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var fileSystem1 = new FileSystem();
            _userOutput = new ConsoleUserOutput();
            var consolePasswordReader = new ConsolePasswordReader(_userOutput, passwordRegistry);

            var certificateStore = new CertificateStore(fileSystem1, consolePasswordReader);
            _certificate = certificateStore.ReadOrCreateCertificateFile("mycert.pfx");

            var wrapper = new CryptoWrapper();
            var cryptoContext = new CryptoContext(wrapper);

            var keyServiceStore = new KeyStoreServiceWrapped(cryptoContext);
            var fileSystem = new FileSystem();

            var multiHashAlgorithm = new BLAKE2B_256();
            var addressHelper = new AddressHelper(multiHashAlgorithm);
            var localKeyStore = new LocalKeyStore(consolePasswordReader, cryptoContext, keyServiceStore, fileSystem,
                _logger, addressHelper);
            var keyRegistry = new KeyRegistry();
            _keySigner = new KeySigner(localKeyStore, cryptoContext, keyRegistry);

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);
            var changeTokenProvider = new TtlChangeTokenProvider(10000);
            var messageCorrelationManager = new RpcMessageCorrelationManager(memoryCache, _logger, changeTokenProvider);
            var peerIdValidator = new PeerIdValidator(cryptoContext);
            var nodeRpcClientChannelFactory =
                new NodeRpcClientChannelFactory(_keySigner, messageCorrelationManager, peerIdValidator);

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 4
            };

            var tcpClientEventLoopGroupFactory = new TcpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);

            var handlers = new List<IRpcResponseObserver>
            {
                new GetVersionResponseObserver(_logger),
                new BroadcastRawTransactionResponseObserver(_logger)
            };

            _nodeRpcClientFactory =
                new NodeRpcClientFactory(nodeRpcClientChannelFactory, tcpClientEventLoopGroupFactory, handlers);
        }

        private async Task<NodeSocketInfo> ConnectToRpcNode(int nodeIndex,
            IPeerIdentifier peerIdentifier,
            IRpcNodeConfig nodeRpcConfig)
        {
            var retry = 5;
            while (retry > 0)
            {
                try
                {
                    _logger.Information($"Connecting to {peerIdentifier.Ip}:{peerIdentifier.Port}");
                    var socket = await _nodeRpcClientFactory.GetClient(_certificate, nodeRpcConfig);
                    socket.SubscribeToResponse<BroadcastRawTransactionResponse>(response =>
                    {
                        _userOutput.WriteLine($"[{nodeIndex}] Transaction response: {response.ResponseCode}");
                    });

                    var socketInfo = new NodeSocketInfo
                    { NodeRpcClient = socket, PeerIdentifier = peerIdentifier };
                    return socketInfo;
                }
                catch (ConnectException)
                {
                    _logger.Error("Connection failed retying...");
                    retry--;
                    await Task.Delay(5000);
                }
            }

            _logger.Error($"Could not connect to {peerIdentifier.Ip}:{peerIdentifier.Port}");

            return null;
        }

        public async Task Simulate(IRpcNodeConfig simulationClientRpcConfig,
            List<IPeerIdentifier> simulationNodePeerIdentifiers)
        {
            var isRunning = true;
            var nodeSocketInfo = new List<NodeSocketInfo>();

            var sender = PeerIdentifier.BuildPeerIdFromConfig(simulationClientRpcConfig);
            foreach (var simulationNodePeerIdentifier in simulationNodePeerIdentifiers)
            {
                var nodeIndex = nodeSocketInfo.Count;
                var nodeRpcConfig = new NodeRpcConfig
                {
                    HostAddress = simulationNodePeerIdentifier.Ip,
                    Port = simulationNodePeerIdentifier.Port,
                    PublicKey = simulationNodePeerIdentifier.PublicKey.KeyToString()
                };

                var socketInfo = await ConnectToRpcNode(nodeIndex, simulationNodePeerIdentifier, nodeRpcConfig)
                   .ConfigureAwait(false);
                if (socketInfo == null)
                {
                    continue;
                }

                nodeSocketInfo.Add(socketInfo);
            }

            var dtoFactory = new DtoFactory();

            await Task.Run(async () =>
            {
                while (isRunning)
                {
                    var guid = Guid.NewGuid();
                    var randomNodeIndex = _random.Next(nodeSocketInfo.Count);
                    var nodeInfo = nodeSocketInfo[randomNodeIndex];

                    var req = new BroadcastRawTransactionRequest();
                    var transaction = new TransactionBroadcast();

                    var stTransactionEntry = new STTransactionEntry();

                    stTransactionEntry.PubKey = ByteString.FromBase64("eOzdzqY+BQHCu6Puno48ujea1Sb+696E31qH21qLmg8=");

                    //stTransactionEntry.PubKey = sender.PublicKey.ToByteString();
                    stTransactionEntry.Amount = _random.Next(100);
                    transaction.STEntries.Add(stTransactionEntry);

                    transaction.Signature = new TransactionSignature
                    {
                        SchnorrSignature = ByteString.CopyFromUtf8($"Signature{guid}"),
                        SchnorrComponent = ByteString.CopyFromUtf8($"Component{guid}")
                    };
                    req.Transaction = transaction;

                    var messageDto = dtoFactory.GetDto(
                        req.ToProtocolMessage(sender.PeerId), sender,
                        nodeInfo.PeerIdentifier);

                    _userOutput.WriteLine($"[{randomNodeIndex}] Sending transaction");

                    nodeInfo.NodeRpcClient.SendMessage(messageDto);

                    await Task.Delay(500).ConfigureAwait(false);
                }
            });
        }
    }
}
