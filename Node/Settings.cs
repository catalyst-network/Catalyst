using System;
using System.IO;
using System.Linq;
using System.Net;
using ADL.Node.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ADL.Rpc.Server;

namespace ADL.Node
{
   
    public class Settings
    {
        public INodeConfiguration Sections { get; private set; }
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
        private Settings(IConfiguration section)
        {
            Sections = new SettingSections
            {
                RPC = new RpcSettings(section.GetSection("RPC")),
                P2P = new P2PSettings(section.GetSection("P2P")),
                Paths =new PathSettings(section.GetSection("Paths")),
                Protocol = new ProtocolSettings(section.GetSection("Protocol"))
            };            
        }

        /// <summary>
        /// Object to hold setting sections.
        /// </summary>
        private struct SettingSections : INodeConfiguration
        {
            public IProtocolSettings Protocol { get; set; }
            public IPathSettings Paths { get; set; }
            public IP2PSettings P2P { get; set; }
            public IRpcSettings RPC { get; set; }
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
        /// Path settings class.
        /// Holds the local storage locations.
        /// </summary>
        private class PathSettings : IPathSettings
        {
            public string Chain { get; private set; }
            public string Index { get; private set; }

            /// <summary>
            /// Sets the chain and index path locations.
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
            public ushort Port { get; private set; }

            /// <summary>
            /// Sets the p2p settings.
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
            ///  Sets RPC Server settings.
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
            public uint Magic { get; private set; }
            public byte AddressVersion { get; private set; }
            public string[] SeedList { get; private set; }

            /// <summary>
            /// Sets the protocol settings.
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
