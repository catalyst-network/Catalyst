using System;
using System.IO;
using System.Linq;
using System.Net;
using ADL.Node.Interfaces;
using ADL.Rpc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ADL.Node
{
   
    public class Settings
    {
        internal INodeConfiguration Sections { get; private set; }
        public static Settings Default { get; private set; }
        private static string ConfigFileLocation { get; set; }
        
        static Settings()
        {
            var env = Environment.GetEnvironmentVariable("ATLASENV");
            
            switch (env)
            {
                case "devnet":
                    ConfigFileLocation = "/Configs/config.devnet.json";
                    break;
                case "testnet":
                    ConfigFileLocation = "/Configs/config.testnet.json";
                    break;
                case "mainnet":
                    ConfigFileLocation = "/Configs/config.mainnet.json";
                    break;
                default:
                    ConfigFileLocation = "/Configs/config.devnet.json";
                    break;
            }
            
            var section = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory()+"/../Node"+ConfigFileLocation)
                .Build()
                .GetSection("ApplicationConfiguration");
            
            Default = new Settings(section);
        }
        
        /// <summary>
        /// Settings constructor
        /// </summary>
        /// <param name="section"></param>
        private Settings(IConfiguration section)
        {
            Sections = new ServiceSettings
            {
                Persistance =new LedgerSettings(section.GetSection("Ledger")),
                Consensus = new ConsensusSettings(section.GetSection("Services").GetSection("Consensus")),
                Contract = new ContractSettings(section.GetSection("Services").GetSection("Contract")),
                Dfs = new DfsSettings(section.GetSection("Services").GetSection("Dfs")),
                Gossip = new GossipSettings(section.GetSection("Services").GetSection("Gossip")),
                Mempool = new MempoolSettings(section.GetSection("Services").GetSection("Mempool")),
                P2P = new P2PSettings(section.GetSection("Services").GetSection("P2P")),
                Rpc = new RpcSettings(section.GetSection("Services").GetSection("Rpc")),
                Protocol = new ProtocolSettings(section.GetSection("Protocol")),
            };            
        }

        /// <summary>
        /// Object to hold setting sections.
        /// </summary>
        private struct ServiceSettings : INodeConfiguration
        {
            public IConsensusSettings Consensus { get; set; }
            public IContractSettings Contract { get; set; }
            public IDfsSettings Dfs { get; set; }
            public IGossipSettings Gossip { get; set; }
            public IMempoolSettings Mempool { get; set; }
            public IP2PSettings P2P { get; set; }
            public IRpcSettings Rpc { get; set; }
            public IProtocolSettings Protocol { get; set; }
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
            return JsonConvert.SerializeObject(Sections, serializerSettings);
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
                Console.WriteLine(section.GetSection("Ledger"));
                Type = section.GetSection("Persistence").Value;
                Chain = section.GetSection("Paths").GetSection("Chain").Value;
                Index = section.GetSection("Paths").GetSection("Index").Value;
            }
        }
        
        /// <summary>
        /// Consensus settings class.
        /// Holds the local storage locations.
        /// </summary>
        private class ConsensusSettings : IConsensusSettings
        {
            public string nDepth { get; set; }

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal ConsensusSettings(IConfiguration section)
            {
                nDepth = section.GetSection("nDepth").Value;
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

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal DfsSettings(IConfiguration section)
            {
                StorageType = section.GetSection("StorageType").Value;
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

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal P2PSettings(IConfiguration section)
            {
                Port = ushort.Parse(section.GetSection("Port").Value);
            }
        }

        /// <summary>
        /// RPC settings class.
        /// </summary>
        private class RpcSettings : IRpcSettings
        {
            public string BindAddress { get; set; }
            public ushort Port { get; set; }
            public string SslCert { get; set; }
            public string SslCertPassword { get; set; }

            /// <summary>
            /// Set attributes
            /// </summary>
            /// <param name="section"></param>
            protected internal RpcSettings(IConfiguration section)
            {
                BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value).ToString();
                Port = ushort.Parse(section.GetSection("Port").Value);
                SslCert = section.GetSection("SslCert").Value;
                SslCertPassword = section.GetSection("SslCertPassword").Value;
            }
        }
        
        /// <summary>
        /// Protocol settings class.
        /// </summary>
        private class ProtocolSettings : IProtocolSettings
        {
            public uint Magic { get; set; }
            public byte AddressVersion { get; set; }
            public string[] SeedList { get; set; }

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
