using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Catalyst.Node.Core.Helpers.Network;
using Dawn;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SharpRepository.Repository;
using SharpRepository.Repository.Configuration;

namespace Catalyst.Node.Core
{
    public sealed class NodeOptions
    {
        public enum Enviroments
        {
            debug = 1,
            test = 2,
            benchmark = 3,
            simulation = 4,
            prod = 5
        }

        public enum Networks
        {
            devnet = 1,
            testnet = 2,
            mainnet = 3
        }

        private static readonly object Mutex = new object();

        /// <summary>
        ///     Settings constructor
        /// </summary>
        /// <param name="env"></param>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <param name="platform"></param>
        /// <param name="persistenceConfiguration"></param>
        private NodeOptions(int env, string dataDir, int network, int platform, ISharpRepositoryConfiguration persistenceConfiguration)
        {
            Env = env;
            DataDir = dataDir;
            Network = network;
            Platform = platform;
            PersistenceConfiguration = persistenceConfiguration;
        }

        private static NodeOptions Instance { get; set; }
        public int Env { get; set; }
        public int Network { get; set; }
        public int Platform { get; set; }
        public string DataDir { get; set; }
        public DfsSettings DfsSettings { get; internal set; }
        public PeerSettings PeerSettings { get; internal set; }
        public WalletSettings WalletSettings { get; internal set; }
        public LedgerSettings LedgerSettings { get; internal set; }
        public MempoolSettings MempoolSettings { get; internal set; }
        public ContractSettings ContractSettings { get; internal set; }
        public ConsensusSettings ConsensusSettings { get; internal set; }
        public readonly ISharpRepositoryConfiguration PersistenceConfiguration;

        /// <summary>
        ///     Get a thread safe settings singleton.
        /// </summary>
        /// <param name="environment"></param>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <param name="platform"></param>
        /// <param name="persistenceConfiguration"></param>
        /// <returns></returns>
        internal static NodeOptions GetInstance(string environment, string dataDir, string network, int platform, ISharpRepositoryConfiguration persistenceConfiguration)
        {
            Guard.Argument(platform, nameof(platform)).InRange(1, 3);
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(environment, nameof(environment)).NotNull().NotEmpty().NotWhiteSpace();

            if (Instance == null)
                lock (Mutex)
                {
                    Instance = Instance == null
                                   ? new NodeOptions(
                                       (int) (Enviroments) Enum.Parse(typeof(Enviroments), environment),
                                       dataDir,
                                       (int) (Networks) Enum.Parse(typeof(Networks), network),
                                       platform,
                                       persistenceConfiguration
                                   )
                                   : throw new ArgumentException();
                }

            return Instance;
        }

        /// <summary>
        ///     Serialises setting section to a json string.
        /// </summary>
        /// <returns></returns>
        public string SerializeSettings()
        {
            var serializerSettings = new JsonSerializerSettings
                                     {
                                         Formatting = Formatting.Indented,
                                         ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                     };
            return JsonConvert.SerializeObject(this, serializerSettings);
        }
    }

    public class NodeOptionsBuilder
    {
        private readonly List<Action<NodeOptions>> _builderActions;
        private readonly IConfiguration _networkConfiguration;
        private readonly NodeOptions _nodeOptions;
        private ISharpRepositoryConfiguration _persistenceConfiguration;
        
        /// <summary>
        /// </summary>
        /// <param name="env"></param>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <param name="platform"></param>
        /// <exception cref="ArgumentException"></exception>
        public NodeOptionsBuilder(string env, string dataDir, string network, int platform)
        {
            Guard.Argument(platform, nameof(platform)).InRange(1, 3);
            Guard.Argument(env, nameof(env)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();

            _builderActions = new List<Action<NodeOptions>>();

            if (!ValidEnvPram(env) || !ValidNetworkParam(network)) throw new ArgumentException();

            _networkConfiguration = LoadNetworkConfig(network, dataDir);
            
            _persistenceConfiguration = RepositoryFactory.BuildSharpRepositoryConfiguation(
                _networkConfiguration.GetSection("PersistenceConfiguration")
            );

            _nodeOptions = NodeOptions.GetInstance(env, dataDir, network, platform, _persistenceConfiguration);
        }

        /// <summary>
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        private bool ValidNetworkParam(string network)
        {
            switch (network)
            {
                case "devnet":
                    return true;
                case "testnet":
                    return true;
                case "mainnet":
                    return true;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        private bool ValidEnvPram(string env)
        {
            switch (env)
            {
                case "debug":
                    return true;
                case "test":
                    return true;
                case "benchmark":
                    return true;
                case "simulation":
                    return true;
                case "prod":
                    return true;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadDfsSettings()
        {
            _builderActions.Add(n => n.DfsSettings = new DfsSettings(_networkConfiguration.GetSection("Dfs")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadPeerSettings()
        {
            _builderActions.Add(n => n.PeerSettings = new PeerSettings(_networkConfiguration.GetSection("Peer")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadContractSettings()
        {
            _builderActions.Add(n => n.ContractSettings =
                                         new ContractSettings(_networkConfiguration.GetSection("Contract")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadMempoolSettings()
        {
            _builderActions.Add(n => n.MempoolSettings =
                                         new MempoolSettings(_networkConfiguration.GetSection("Mempool")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadLedgerSettings()
        {
            _builderActions.Add(n => n.LedgerSettings = new LedgerSettings(_networkConfiguration.GetSection("Ledger")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadConsensusSettings()
        {
            _builderActions.Add(n => n.ConsensusSettings =
                                         new ConsensusSettings(_networkConfiguration.GetSection("Consensus")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadWalletSettings()
        {
            _builderActions.Add(n => n.WalletSettings = new WalletSettings(_networkConfiguration.GetSection("Wallet")));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public NodeOptionsBuilder When(Func<bool> condition)
        {
            var result = condition.Invoke();

            if (!result)
            {
                var oldAction = _builderActions[_builderActions.Count - 1];
                _builderActions.Remove(oldAction);
            }

            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public NodeOptions Build()
        {
            _builderActions.ForEach(ba => ba(_nodeOptions));
            return _nodeOptions;
        }

        /// <summary>
        ///     Loads config files from defined dataDir
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private IConfiguration LoadNetworkConfig(string network, string dataDir)
        {
            Guard.Argument(dataDir, nameof(dataDir)).NotNull().NotEmpty().NotWhiteSpace();
            Guard.Argument(network, nameof(network)).NotNull().NotEmpty().NotWhiteSpace();

            string networkConfigFile;

            switch (network)
            {
                case "devnet":
                    networkConfigFile = "devnet.json";
                    break;
                case "mainnet":
                    networkConfigFile = "mainnet.json";
                    break;
                case "testnet":
                    networkConfigFile = "testnet.json";
                    break;
                default:
                    networkConfigFile = "devnet.json";
                    break;
            }

            return new ConfigurationBuilder()
                  .AddJsonFile($"/{dataDir}/{networkConfigFile}")
                  .Build()
                  .GetSection("CatalystNodeConfiguration");
        }
    }

    /// <summary>
    ///     Persistence settings class.
    /// </summary>
    public class LedgerSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal LedgerSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            Type = section.GetSection("Persistence").Value;
        }

        public string Type { get; set; }
    }

    /// <summary>
    ///     Consensus settings class.
    /// </summary>
    public class ConsensusSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal ConsensusSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            NDepth = section.GetSection("nDepth").Value;
        }

        public string NDepth { get; set; }
    }

    /// <summary>
    ///     Contract settings class.
    /// </summary>
    public class ContractSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal ContractSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            StorageType = section.GetSection("StorageType").Value;
        }

        public string StorageType { get; set; }
    }

    /// <summary>
    ///     Dfs settings class.
    /// </summary>
    public class DfsSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal DfsSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            StorageType = section.GetSection("StorageType").Value;
            ConnectRetries = ushort.Parse(section.GetSection("ConnectRetries").Value);
            IpfsVersionApi = section.GetSection("IpfsVersionApi").Value;
        }

        public string StorageType { get; set; }
        public ushort ConnectRetries { get; set; }
        public string IpfsVersionApi { get; set; }
    }

    /// <summary>
    ///     wallet settings class.
    /// </summary>
    public class WalletSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal WalletSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            WalletRpcIp = IPAddress.Parse(section.GetSection("WalletRpcIp").Value);
            Guard.Argument(WalletRpcIp, nameof(WalletRpcIp)).NotNull();
            WalletRpcPort = int.Parse(section.GetSection("WalletRpcPort").Value);
            Guard.Argument(WalletRpcPort, nameof(WalletRpcPort)).Min(1025).Max(65535);
        }

        public int WalletRpcPort { get; set; }
        public IPAddress WalletRpcIp { get; set; }
    }

    /// <summary>
    ///     Mempool settings class.
    /// </summary>
    public class MempoolSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal MempoolSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            Type = section.GetSection("Type").Value;
            When = section.GetSection("When").Value;
            Host = EndpointBuilder.BuildNewEndPoint(
                IPAddress.Parse(section.GetSection("Host").Value),
                int.Parse(section.GetSection("Port").Value)
            );
        }

        public string Type { get; set; }
        public string When { get; set; }
        public IPEndPoint Host { get; set; }
    }

    /// <summary>
    ///     Peer settings class.
    /// </summary>
    public class PeerSettings
    {
        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal PeerSettings(IConfiguration section)
        {
            Guard.Argument(section, nameof(section)).NotNull();
            Network = section.GetSection("Network").Value;
            PublicKey = section.GetSection("PublicKey").Value;
            Port = int.Parse(section.GetSection("Port").Value);
            Magic = int.Parse(section.GetSection("Magic").Value);
            PfxFileName = section.GetSection("PfxFileName").Value;
            PayoutAddress = section.GetSection("PayoutAddress").Value;
            Announce = bool.Parse(section.GetSection("Announce").Value);
            SslCertPassword = section.GetSection("SslCertPassword").Value;
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            MaxConnections = ushort.Parse(section.GetSection("MaxConnections").Value);
            AcceptInvalidCerts = bool.Parse(section.GetSection("AcceptInvalidCerts").Value);
            MutualAuthentication = bool.Parse(section.GetSection("MutualAuthentication").Value);
            DnsServer = EndpointBuilder.BuildNewEndPoint(section.GetSection("DnsServer").Value);
            KnownNodes = section.GetSection("KnownNodes").GetChildren().Select(p => p.Value).ToList();
            SeedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
            AnnounceServer =
                Announce ? EndpointBuilder.BuildNewEndPoint(section.GetSection("AnnounceServer").Value) : null;
        }

        public string Network { get; set; }
        public string PayoutAddress { get; set; }
        public string PublicKey { get; set; }
        public bool Announce { get; set; }
        public IPEndPoint DnsServer { get; set; }
        public IPEndPoint AnnounceServer { get; set; }
        public bool MutualAuthentication { get; set; }
        public bool AcceptInvalidCerts { get; set; }
        public ushort MaxConnections { get; set; }
        public int Port { get; set; }
        public int Magic { get; set; }
        public IPAddress BindAddress { get; set; }
        public string PfxFileName { get; set; }
        public List<string> KnownNodes { get; set; }
        public List<string> SeedServers { get; set; }
        public byte AddressVersion { get; set; }
        public string SslCertPassword { get; set; }
    }
}