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

using System.Collections.Generic;
using System.Net;
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
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Simulator
{
    public class Simulator
    {
        private readonly ILogger _logger;
        private readonly FileSystem _fileSystem;
        private readonly ConsolePasswordReader _consolePasswordReader;
        private IPeerIdentifier _sender;
        private IPeerIdentifier _recipient;
        private INodeRpcClient _client;
        private readonly NodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ConsoleUserOutput _userOutput;

        public Simulator(PasswordRegistry passwordRegistry)
        {
            _logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            _fileSystem = new FileSystem();
            _userOutput = new ConsoleUserOutput();
            _consolePasswordReader = new ConsolePasswordReader(_userOutput, passwordRegistry);

            var wrapper = new CryptoWrapper();
            var cryptoContext = new CryptoContext(wrapper);

            var keyServiceStore = new KeyStoreServiceWrapped(cryptoContext);
            var fileSystem = new FileSystem();
            
            var multiHashAlgorithm = new BLAKE2B_256();
            var addressHelper = new AddressHelper(multiHashAlgorithm);
            var localKeyStore = new LocalKeyStore(_consolePasswordReader, cryptoContext, keyServiceStore, fileSystem, _logger, addressHelper);
            var keyRegistry = new KeyRegistry();
            var keySigner = new KeySigner(localKeyStore, cryptoContext, keyRegistry);

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);
            var changeTokenProvider = new TtlChangeTokenProvider(10000);
            var messageCorrelationManager = new RpcMessageCorrelationManager(memoryCache, _logger, changeTokenProvider);
            var peerIdValidator = new PeerIdValidator(cryptoContext);
            var nodeRpcClientChannelFactory =
                new NodeRpcClientChannelFactory(keySigner, messageCorrelationManager, peerIdValidator);

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpClientHandlerWorkerThreads = 2
            };

            var tcpClientEventLoopGroupFactory = new TcpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);

            var handlers = new List<IRpcResponseObserver>
                {new BroadcastRawTransactionResponseObserver(_logger)};

            _nodeRpcClientFactory =
                new NodeRpcClientFactory(nodeRpcClientChannelFactory, tcpClientEventLoopGroupFactory, handlers);
        }

        public async Task Simulate()
        {
            var nodeRpcConfig = new NodeRpcConfig
            {
                NodeId = "Node1",
                HostAddress = IPAddress.Loopback,
                Port = 42066,
                PfxFileName = "mycert.pfx",
                SslCertPassword = "test",
                PublicKey = "1AemkEe4z3rZHr7RWSUyZHPuVozyCQnT1H7SfpzcGCQRuT"
            };

            var clientRpcConfig = new NodeRpcConfig
            {
                HostAddress = IPAddress.Loopback,
                Port = 5266,
                PublicKey = "1AemkEe4z3rZHr7RWSUyZHPuVozyCQnT1H7SfpzcGCQRuT"
            };

            var certificateStore = new CertificateStore(_fileSystem, _consolePasswordReader);
            var certificate = certificateStore.ReadOrCreateCertificateFile(nodeRpcConfig.PfxFileName);

            _client = await _nodeRpcClientFactory.GetClient(certificate, nodeRpcConfig);

            _sender = PeerIdentifier.BuildPeerIdFromConfig(clientRpcConfig);
            _recipient = PeerIdentifier.BuildPeerIdFromConfig(nodeRpcConfig);

            var dtoFactory = new DtoFactory();

            _client.SubscribeToResponse<BroadcastRawTransactionResponse>(response =>
            {
                _userOutput.WriteLine($"Transaction response: {response.ResponseCode}");
            });

            var i = 0;
            await Task.Run(async () =>
            {
                while (true)
                {
                    var req = new BroadcastRawTransactionRequest();
                    var transaction = new TransactionBroadcast();
                    transaction.Signature = new TransactionSignature
                    {
                        SchnorrSignature = ByteString.CopyFromUtf8($"Signature{i}"),
                        SchnorrComponent = ByteString.CopyFromUtf8($"Component{i}")
                    };
                    req.Transaction = transaction;

                    var messageDto = dtoFactory.GetDto(req.ToProtocolMessage(_sender.PeerId), _sender, _recipient);

                    _userOutput.WriteLine("Sending transaction");
                    _client.SendMessage(messageDto);
                    i++;
                    await Task.Delay(500);
                }
            });
        }
    }
}
