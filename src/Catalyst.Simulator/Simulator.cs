using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileSystem;
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
using Microsoft.Extensions.Caching.Memory;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Simulator
{
    public class Simulator
    {
        private readonly FileSystem _fileSystem;
        private readonly ConsolePasswordReader _consolePasswordReader;
        private IPeerIdentifier _sender;
        private IPeerIdentifier _recipient;
        private INodeRpcClient _client;
        private readonly NodeRpcClientFactory _nodeRpcClientFactory;
        private readonly ConsoleUserOutput _userOutput;

        public Simulator()
        {
            _fileSystem = new FileSystem();
            _userOutput = new ConsoleUserOutput();

            var passwordRegistry = new PasswordRegistry();
            _consolePasswordReader = new ConsolePasswordReader(_userOutput, passwordRegistry);

            var wrapper = new CryptoWrapper();
            var cryptoContext = new CryptoContext(wrapper);

            var keyServiceStore = new KeyStoreServiceWrapped(cryptoContext);
            var fileSystem = new FileSystem();
            var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            var multiHashAlgorithm = new BLAKE2B_256();
            var addressHelper = new AddressHelper(multiHashAlgorithm);
            var localKeyStore = new LocalKeyStore(_consolePasswordReader, cryptoContext, keyServiceStore, fileSystem,
                logger, addressHelper);
            var keyRegistry = new KeyRegistry();
            var keySigner = new KeySigner(localKeyStore, cryptoContext, keyRegistry);

            var memoryCacheOptions = new MemoryCacheOptions();
            var memoryCache = new MemoryCache(memoryCacheOptions);
            var changeTokenProvider = new TtlChangeTokenProvider(10000);
            var messageCorrelationManager = new RpcMessageCorrelationManager(memoryCache, logger, changeTokenProvider);
            var peerIdValidator = new PeerIdValidator(cryptoContext);
            var nodeRpcClientChannelFactory =
                new NodeRpcClientChannelFactory(keySigner, messageCorrelationManager, peerIdValidator);

            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration();
            eventLoopGroupFactoryConfiguration.TcpClientHandlerWorkerThreads = 2;
            var tcpClientEventLoopGroupFactory = new TcpClientEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);

            var handlers = new[] {new GetVersionResponseObserver(logger)};
            _nodeRpcClientFactory = new NodeRpcClientFactory(nodeRpcClientChannelFactory, tcpClientEventLoopGroupFactory, handlers);
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
            _client.SubscribeToResponse<VersionResponse>(response =>
            {
                _userOutput.WriteLine("Received message");
                var a = 0;
            });

            var messageDto = dtoFactory.GetDto(new VersionRequest().ToProtocolMessage(_sender.PeerId), _sender, _recipient);

            await Task.Run(async () =>
            {
                while (true)
                {
                    _userOutput.WriteLine("Sending message");
                    _client.SendMessage(messageDto);
                    await Task.Delay(500);
                }
            });
        }
    }
}
