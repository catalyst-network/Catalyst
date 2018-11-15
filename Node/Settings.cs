using System;
using System.IO;
using System.Linq;
using System.Net;
using ADL.Node.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    internal class Settings : INodeConfiguration
    {
        public IProtocolSettings Protocol { get; }
        public IPathSettings Paths { get; }
        public IP2PSettings P2P { get; }
        public IRPCSettings RPC { get; }
        
        public static Settings Default { get; private set; }
        
        public static string ConfigFileLocation { get; private set; }
        
        static Settings()
        {
            string env = Environment.GetEnvironmentVariable("ATLASENV");
            
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
            
            IConfigurationSection section = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory()+"/../Node"+ConfigFileLocation)
                .Build()
                .GetSection("ApplicationConfiguration");
            
            Default = new Settings(section);
        }
        
        /// <summary>
        /// Settings constructor
        /// </summary>
        /// <param name="section"></param>
        protected internal Settings(IConfiguration section)
        {
            Protocol = new ProtocolSettings(section.GetSection("Protocol"));
            Paths = new PathSettings(section.GetSection("Paths"));
            P2P = new P2PSettings(section.GetSection("P2P"));
            RPC = new RpcSettings(section.GetSection("RPC"));
        }

        /// <summary>
        /// Path settings class.
        /// Holds the local storage locations
        /// </summary>
        private class PathSettings : IPathSettings
        {
            private string Chain { get; set; }
            private string Index { get; set; }

            /// <summary>
            /// sets the chain and index path locations
            /// </summary>
            /// <param name="section"></param>
            protected internal PathSettings(IConfiguration section)
            {
                Console.WriteLine(section.GetSection("Chain"));
                Chain = section.GetSection("Chain").Value;
                Index = section.GetSection("Index").Value;
            }
        }

        /// <summary>
        /// P2P settings class.
        /// </summary>
        private class P2PSettings : IP2PSettings
        {
            private ushort Port { get; set; }

            /// <summary>
            /// Sets the p2p settings
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
        private class RpcSettings : IRPCSettings
        {
            private IPAddress BindAddress { get; set; }
            private ushort Port { get; set; }
            private string SslCert { get; set; }
            private string SslCertPassword { get; set; }

            /// <summary>
            ///  Sets RPC Server settings
            /// </summary>
            /// <param name="section"></param>
            protected internal RpcSettings(IConfiguration section)
            {
                BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
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
            private uint Magic { get; }
            private byte AddressVersion { get; }
            private string[] SeedList { get; }

            /// <summary>
            /// Sets the protocol settings
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