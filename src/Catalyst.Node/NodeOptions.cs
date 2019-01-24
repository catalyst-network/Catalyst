using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Catalyst.Helpers.Network;
using Catalyst.Helpers.Util;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Catalyst.Node
{
    public sealed class NodeOptions
    {
        
        private static NodeOptions Instance { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Settings constructor
        /// </summary>
        private NodeOptions(uint env, string dataDir, string network, uint platform)
        {
            Env = env;
            DataDir = dataDir;
            Network = network;
            Platform = platform;
        }
        
        public uint Env { get; set; }
        public uint Platform { get; set; }
        public string Network { get; set; }
        public string DataDir { get; set; }
        public DfsSettings DfsSettings { get; internal set; }
        public PeerSettings PeerSettings { get; internal set; }
        public WalletSettings WalletSettings { get; internal set; }
        public GossipSettings GossipSettings { get; internal set; }
        public LedgerSettings LedgerSettings { get; internal set; }
        public MempoolSettings MempoolSettings { get; internal set; }
        public ContractSettings ContractSettings { get; internal set; }
        public ConsensusSettings ConsensusSettings { get; internal set; }

        /// <summary>
        ///     Get a thread safe settings singleton.
        /// </summary>
        /// <returns></returns>
        internal static NodeOptions GetInstance(uint env, string dataDir, string network, uint platform)
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new NodeOptions(env, dataDir, network, platform);
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
        private NodeOptions _nodeOptions;
        private IConfiguration _networkConfiguration;
        private readonly List<Action<NodeOptions>> _builderActions;

        public NodeOptionsBuilder(uint env, string dataDir, string network, uint platform)
        {
            _networkConfiguration = LoadNetworkConfig(network, dataDir);
            _nodeOptions = NodeOptions.GetInstance(env, dataDir, network, platform);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadDfsSettings()
        {
            _builderActions.Add(n => n.DfsSettings = new DfsSettings(_networkConfiguration.GetSection("Dfs")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadPeerSettings()
        {
            _builderActions.Add(n => n.PeerSettings = new PeerSettings(_networkConfiguration.GetSection("Peer")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadContractSettings()
        {
            _builderActions.Add(n => n.ContractSettings = new ContractSettings(_networkConfiguration.GetSection("Contract")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadMempoolSettings()
        {
            _builderActions.Add(n => n.MempoolSettings = new MempoolSettings(_networkConfiguration.GetSection("Mempool")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadLedgerSettings()
        {
            _builderActions.Add(n => n.LedgerSettings = new LedgerSettings(_networkConfiguration.GetSection("Ledger")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadGossipSettings()
        {
            _builderActions.Add(n => n.GossipSettings = new GossipSettings(_networkConfiguration.GetSection("Gossip")));
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadConsensusSettings()
        {
            _builderActions.Add(n => n.ConsensusSettings = new ConsensusSettings(_networkConfiguration.GetSection("Consensus")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder LoadWalletSettings()
        {
            _builderActions.Add(n => n.WalletSettings = new WalletSettings(_networkConfiguration.GetSection("Wallet")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public NodeOptionsBuilder Override()
        {
            _builderActions.Add(n => n.DfsSettings = new DfsSettings(_networkConfiguration.GetSection("Dfs")));
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public NodeOptionsBuilder When(Func<Boolean> condition)
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
        /// 
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
            Guard.NotNull(dataDir, nameof(dataDir));
            Guard.NotNull(network, nameof(network));

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
                .AddJsonFile($"{dataDir}/{networkConfigFile}")
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
            Guard.NotNull(section, nameof(section));
            Type = section.GetSection("Persistence").Value;
            Chain = section.GetSection("Paths").GetSection("Chain").Value;
            Index = section.GetSection("Paths").GetSection("Index").Value;
        }

        public string Type { get; set; }
        public string Chain { get; set; }
        public string Index { get; set; }
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
            Guard.NotNull(section, nameof(section));
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
            Guard.NotNull(section, nameof(section));
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
            Guard.NotNull(section, nameof(section));
            StorageType = section.GetSection("StorageType").Value;
            ConnectRetries = ushort.Parse(section.GetSection("ConnectRetries").Value);
            IpfsVersionApi = section.GetSection("IpfsVersionApi").Value;
        }

        public string StorageType { get; set; }
        public ushort ConnectRetries { get; set; }
        public string IpfsVersionApi { get; set; }
    }

    /// <summary>
    ///     Gossip settings class.
    /// </summary>
    public class GossipSettings
    {

        /// <summary>
        ///     Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal GossipSettings(IConfiguration section)
        {
            Guard.NotNull(section, nameof(section));
            Instances = section.GetSection("instances").Value;
        }

        public string Instances { get; set; }
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
            Guard.NotNull(section, nameof(section));
            WalletRpcIp = IPAddress.Parse(section.GetSection("WalletRpcIp").Value);
            Guard.NotNull(WalletRpcIp, nameof(WalletRpcIp));
            WalletRpcPort = uint.Parse(section.GetSection("WalletRpcPort").Value);

            if (!Ip.ValidPortRange(WalletRpcPort))
            {
                WalletRpcPort = 42444; 
            }
        }
        public uint WalletRpcPort { get; set; }
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
            Guard.NotNull(section, nameof(section));
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
            Guard.NotNull(section, nameof(section));
            Network = section.GetSection("Network").Value;
            PublicKey = section.GetSection("PublicKey").Value;
            Port = int.Parse(section.GetSection("Port").Value);
            Magic = uint.Parse(section.GetSection("Magic").Value);
            PfxFileName = section.GetSection("PfxFileName").Value;
            PayoutAddress = section.GetSection("PayoutAddress").Value;
            Announce = bool.Parse(section.GetSection("Announce").Value);
            SslCertPassword = section.GetSection("SslCertPassword").Value;
            MaxConnections = ushort.Parse(section.GetSection("MaxPeers").Value);
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            PingInterval = ushort.Parse(section.GetSection("PeerPingInterval").Value);
            AcceptInvalidCerts = bool.Parse(section.GetSection("AcceptInvalidCerts").Value);
            MutualAuthentication = bool.Parse(section.GetSection("MutualAuthentication").Value);
            KnownNodes = section.GetSection("KnownNodes").GetChildren().Select(p => p.Value).ToList();
            SeedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
            AnnounceServer = Announce ? EndpointBuilder.BuildNewEndPoint(section.GetSection("AnnounceServer").Value) : null;
        }

        public string Network { get; set; }
        public string PayoutAddress { get; set; }
        public string PublicKey { get; set; }
        public bool Announce { get; set; }
        public IPEndPoint AnnounceServer { get; set; }
        public bool MutualAuthentication { get; set; }
        public bool AcceptInvalidCerts { get; set; }
        public ushort MaxConnections { get; set; }
        public ushort PingInterval { get; set; }
        public int Port { get; set; }
        public uint Magic { get; set; }
        public IPAddress BindAddress { get; set; }
        public string PfxFileName { get; set; }
        public List<string> KnownNodes { get; set; }
        public List<string> SeedServers { get; set; }
        public byte AddressVersion { get; set; }
        public string SslCertPassword { get; set; }
    }
}
