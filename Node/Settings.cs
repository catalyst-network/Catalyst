using System;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using ADL.Node.Core.Modules.Dfs;
using ADL.Node.Core.Modules.Rpc;
using ADL.Node.Core.Modules.Gossip;
using ADL.Node.Core.Modules.Ledger;
using ADL.Node.Core.Modules.Network;
using ADL.Node.Core.Modules.Mempool;
using ADL.Node.Core.Modules.Contract;
using ADL.Node.Core.Modules.Consensus;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    public sealed class Settings
    {
        public IRpcSettings Rpc { get; set; }
        public IDfsSettings Dfs { get; set; }
        public ISslSettings Ssl  { get; set; }
        public INetworkSettings Peer { get; set; }
        public IGossipSettings Gossip { get; set; }
        public ILedgerSettings Ledger { get; set; }
        public NodeOptions NodeOptions { get; set; }
        public IMempoolSettings Mempool { get; set; }
        public IContractSettings Contract { get; set; }
        public IConsensusSettings Consensus { get; set; }
       
        private static Settings Instance { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get a thread safe settings singleton.
        /// </summary>
        /// <returns></returns>
        public static Settings GetInstance(NodeOptions options)
        { 
            if (Instance == null) 
            { 
                lock (Mutex)
                {
                    if (Instance == null) 
                    { 
                        Instance = new Settings(options);
                    }
                } 
            }
            return Instance;
        }
        
        /// <summary>
        /// Settings constructor
        /// </summary>
        /// <param name="options"></param>
        private Settings(NodeOptions options)
        {
            var userConfig = LoadConfig(options.DataDir, options.Network);

            NodeOptions = options;
            Ssl = new SslSettings(userConfig.GetSection("Ssl"));
            Dfs = new DfsSettings(userConfig.GetSection("Dfs"));
            Rpc = new RpcSettings(userConfig.GetSection("Rpc"));
            Peer = new NetworkSettings(userConfig.GetSection("Peer"));
            Gossip = new GossipSettings(userConfig.GetSection("Gossip"));
            Ledger = new LedgerSettings(userConfig.GetSection("Ledger"));
            Mempool = new MempoolSettings(userConfig.GetSection("Mempool"));
            Contract = new ContractSettings(userConfig.GetSection("Contract"));
            Consensus = new ConsensusSettings(userConfig.GetSection("Consensus"));

        }

        /// <summary>
        /// Loads config files from defined dataDir
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private static IConfiguration LoadConfig(string dataDir, string network)
        {
#if DEBUG
            Console.WriteLine("Load Config parameters");
            Console.WriteLine(dataDir);
#endif
            string networkConfigFile;
            
            switch (network)
            {
                case "devnet":
                    networkConfigFile = "/devnet.json";
                    break;
                case "mainnet":
                    networkConfigFile = "/mainnet.json";
                    break;
                case "testnet":
                    networkConfigFile = "/testnet.json";
                    break;
                default:
                    networkConfigFile = "/devnet.json";
                    break;
            }
           
            return new ConfigurationBuilder()
                .AddJsonFile(dataDir+networkConfigFile)
                .Build()
                .GetSection("ApplicationConfiguration");
        }
        
        /// <summary>
        /// Serialises setting section to a json string.
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
    
    /// <summary>
    /// Persistence settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class LedgerSettings : ILedgerSettings
    {
        public string Type { get; set; }
        public string Chain { get; set; }
        public string Index { get; set; }

        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal LedgerSettings(IConfiguration section)
        {
            Type = section.GetSection("Persistence").Value;
            Chain = section.GetSection("Paths").GetSection("Chain").Value;
            Index = section.GetSection("Paths").GetSection("Index").Value;
        }
    }

//        
    /// <summary>
    /// Consensus settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class ConsensusSettings : IConsensusSettings
    {
        public string NDepth { get; set; }

        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal ConsensusSettings(IConfiguration section)
        {
            NDepth = section.GetSection("nDepth").Value;
        }
    }
        
    /// <summary>
    /// Contract settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class ContractSettings : IContractSettings
    {
        public string StorageType { get; set; }

        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal ContractSettings(IConfiguration section)
        {
            StorageType = section.GetSection("StorageType").Value;
        }
    }
        
    /// <summary>
    /// Dfs settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class DfsSettings : IDfsSettings
    {
        public string StorageType { get; set; }
        public ushort ConnectRetries { get; set; }
        public string IpfsVersionApi { get; set; }
            
        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal DfsSettings(IConfiguration section)
        {
            StorageType = section.GetSection("StorageType").Value;
            ConnectRetries = ushort.Parse(section.GetSection("ConnectRetries").Value);
            IpfsVersionApi = section.GetSection("IpfsVersionApi").Value;
        }
    }

    /// <summary>
    /// Gossip settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class GossipSettings : IGossipSettings
    {
        public string Instances { get; set; }

        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal GossipSettings(IConfiguration section)
        {
            Instances = section.GetSection("instances").Value;
        }
    }        
        
    /// <summary>
    /// Mempool settings class.
    /// Holds the local storage locations.
    /// </summary>
    public class MempoolSettings : IMempoolSettings
    {
        public string Type { get; set; }
        public string When { get; set; }

        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal MempoolSettings(IConfiguration section)
        {
            Type = section.GetSection("Type").Value;
            When = section.GetSection("When").Value;
        }
    } 
        
    /// <summary>
    /// Peer settings class.
    /// </summary>
    public class NetworkSettings : INetworkSettings
    {
        public string BindAddress { get; set; }
        public int Port { get; set; }
        public ushort MaxPeers { get; set; }
        public ushort PeerPingInterval { get; set; }
        public ushort PeerLifetimeInterval { get; set; }
        public uint Magic { get; set; }
        public string[] SeedList { get; set; }
        public byte AddressVersion { get; set; }
        
        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal NetworkSettings(IConfiguration section)
        {
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value).ToString();
            Magic = uint.Parse(section.GetSection("Magic").Value);
            Port = int.Parse(section.GetSection("Port").Value);
            MaxPeers = ushort.Parse(section.GetSection("MaxPeers").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            PeerPingInterval = ushort.Parse(section.GetSection("PeerPingInterval").Value);
            PeerLifetimeInterval = ushort.Parse(section.GetSection("PeerLifetimeInterval").Value);
            SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
        }
    }

    public interface ISslSettings
    {
        string PfxFileName { get; set; }
        string SslCertPassword { get; set; }  
    }

    /// <summary>
    /// Hold settings for ssl/tls
    /// </summary>
    public class SslSettings : ISslSettings
    {
        public string PfxFileName { get; set; }
        public string SslCertPassword { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="section"></param>
        protected internal SslSettings(IConfiguration section)
        {
            Console.WriteLine(section.GetSection("PfxFileName").Value);
            Console.WriteLine(section.GetSection("SslCertPassword").Value);
            PfxFileName = section.GetSection("PfxFileName").Value;
            SslCertPassword = section.GetSection("SslCertPassword").Value;
        }
    }


    /// <summary>
    /// RPC settings class.
    /// </summary>
    public class RpcSettings : IRpcSettings
    {
        public int Port { get; set; }
        public string BindAddress { get; set; }
        
        /// <summary>
        /// Set attributes
        /// </summary>
        /// <param name="section"></param>
        protected internal RpcSettings(IConfiguration section)
        {
            Port = int.Parse(section.GetSection("Port").Value);
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value).ToString();
        }
    }
}

