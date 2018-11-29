using System;
using ADL.Rpc;
using ADL.P2P;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using ADL.Node.Interfaces;
using ADL.Cryptography.SSL;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    public sealed class Settings
    {
        private static Settings Instance { get; set; }
        private static readonly object Mutex = new object();
        public INodeConfiguration NodeConfiguration { get; set; }

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
            
            NodeConfiguration = new ServiceSettings
            {
                NodeOptions = options,
                Ssl = new SslSettings(userConfig.GetSection("Ssl")),
                Protocol = new ProtocolSettings(userConfig.GetSection("Protocol")),
                Persistance = new LedgerSettings(userConfig.GetSection("Ledger")),
                Dfs = new DfsSettings(userConfig.GetSection("Services").GetSection("Dfs")),
                P2P = new P2PSettings(userConfig.GetSection("Services").GetSection("P2P")),
                Rpc = new RpcSettings(userConfig.GetSection("Services").GetSection("Rpc")),
                Gossip = new GossipSettings(userConfig.GetSection("Services").GetSection("Gossip")),
                Mempool = new MempoolSettings(userConfig.GetSection("Services").GetSection("Mempool")),
                Contract = new ContractSettings(userConfig.GetSection("Services").GetSection("Contract")),
                Consensus = new ConsensusSettings(userConfig.GetSection("Services").GetSection("Consensus"))
            };            
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
                    networkConfigFile = "/config.devnet.json";
                    break;
                case "mainnet":
                    networkConfigFile = "/config.mainnet.json";
                    break;
                case "testnet":
                    networkConfigFile = "/config.testnet.json";
                    break;
                default:
                    networkConfigFile = "/config.devnet.json";
                    break;
            }
           
            return new ConfigurationBuilder()
                .AddJsonFile(dataDir+networkConfigFile)
                .Build()
                .GetSection("ApplicationConfiguration");
        }

        /// <summary>
        /// Object to hold setting sections.
        /// </summary>
        private struct ServiceSettings : INodeConfiguration
        {
            public IP2PSettings P2P { get; set; }
            public ISslSettings Ssl { get; set; }
            public IRpcSettings Rpc { get; set; }
            public IDfsSettings Dfs { get; set; }
            public IGossipSettings Gossip { get; set; }
            public IMempoolSettings Mempool { get; set; }
            public IContractSettings Contract { get; set; }
            public IProtocolSettings Protocol { get; set; }
            public IConsensusSettings Consensus { get; set; }
            public NodeOptions NodeOptions { get; set; }
            public IPersistanceSettings Persistance { get; set; }
            
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
            return JsonConvert.SerializeObject(NodeConfiguration, serializerSettings);
        }

        /// <summary>
        /// Persistence settings class.
        /// Holds the local storage locations.
        /// </summary>
        private class LedgerSettings : IPersistanceSettings
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
        
        /// <summary>
        /// Hold settings for ssl/tls
        /// </summary>
        private class SslSettings : ISslSettings
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
        /// Consensus settings class.
        /// Holds the local storage locations.
        /// </summary>
        private class ConsensusSettings : IConsensusSettings
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
        private class ContractSettings : IContractSettings
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
        private class DfsSettings : IDfsSettings
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
        private class GossipSettings : IGossipSettings
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
        private class MempoolSettings : IMempoolSettings
        {
            public string Type { get; set; }

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal MempoolSettings(IConfiguration section)
            {
                Type = section.GetSection("Type").Value;
            }
        }

        /// <summary>
        /// P2P settings class.
        /// </summary>
        private class P2PSettings : IP2PSettings
        {
            public ushort Port { get; set; }
            public ushort MaxPeers { get; set; }
            public ushort PeerPingInterval { get; set; }
            public ushort PeerLifetimeInterval { get; set; }


            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal P2PSettings(IConfiguration section)
            {
                Port = ushort.Parse(section.GetSection("Port").Value);
                MaxPeers = ushort.Parse(section.GetSection("MaxPeers").Value);
                PeerPingInterval = ushort.Parse(section.GetSection("PeerPingInterval").Value);
                PeerLifetimeInterval = ushort.Parse(section.GetSection("PeerLifetimeInterval").Value);
            }
        }

        /// <summary>
        /// RPC settings class.
        /// </summary>
        private class RpcSettings : IRpcSettings
        {
            public ushort Port { get; set; }
            public string BindAddress { get; set; }

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal RpcSettings(IConfiguration section)
            {
                Port = ushort.Parse(section.GetSection("Port").Value);
                BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value).ToString();
            }
        }
        
        /// <summary>
        /// Protocol settings class.
        /// </summary>
        private class ProtocolSettings : IProtocolSettings
        {
            public uint Magic { get; set; }
            public string[] SeedList { get; set; }
            public byte AddressVersion { get; set; }
            
            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal ProtocolSettings(IConfiguration section)
            {
                Magic = uint.Parse(section.GetSection("Magic").Value);
                AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
                SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
            }
        }
    }
}
