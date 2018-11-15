using System;
using System.IO;
using System.Linq;
using System.Net;
using ADL.Cli.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ADL.Cli
{
    internal class Settings : INodeConfiguration
    {
        public IProtocolSettings Protocol { get; }
        public IPathSettings Paths { get; }
        public IP2PSettings P2P { get; }
        public IRPCSettings RPC { get; }
        
        public static Settings Default { get; private set; }
        
        public static string configFileLocation { get; private set; }
        
        static Settings()
        {
            string env = Environment.GetEnvironmentVariable("ATLASENV");
            
            switch (env)
            {
                case "devnet":
                    configFileLocation = "/Configs/config.devnet.json";
                    break;
                case "testnet":
                    configFileLocation = "/Configs/config.testnet.json";
                    break;
                case "mainnet":
                    configFileLocation = "/Configs/config.mainnet.json";
                    break;
                default:
                    configFileLocation = "/Configs/config.devnet.json";
                    break;
            }
            
            IConfigurationSection section = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory()+configFileLocation)
                .Build()
                .GetSection("ApplicationConfiguration");
            
            Default = new Settings(section);
        }
        
        protected internal Settings(IConfiguration section)
        {
            Protocol = new ProtocolSettings(section.GetSection("Protocol"));
            Paths = new PathSettings(section.GetSection("Paths"));
            P2P = new P2PSettings(section.GetSection("P2P"));
            RPC = new RpcSettings(section.GetSection("RPC"));
        }
//       
        private class PathSettings : IPathSettings
        {
            private string Chain { get; set; }
            private string Index { get; set; }

            protected internal PathSettings(IConfiguration section)
            {
                Console.WriteLine(section.GetSection("Chain"));
                Chain = section.GetSection("Chain").Value;
                Index = section.GetSection("Index").Value;
            }
        }

        private class P2PSettings : IP2PSettings
        {
            private ushort Port { get; set; }

            protected internal P2PSettings(IConfiguration section)
            {
                Port = ushort.Parse(section.GetSection("Port").Value);
            }
        }

        private class RpcSettings : IRPCSettings
        {
            private IPAddress BindAddress { get; set; }
            private ushort Port { get; set; }
            private string SslCert { get; set; }
            private string SslCertPassword { get; set; }

            protected internal RpcSettings(IConfiguration section)
            {
                BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
                Port = ushort.Parse(section.GetSection("Port").Value);
                SslCert = section.GetSection("SslCert").Value;
                SslCertPassword = section.GetSection("SslCertPassword").Value;
            }
        }
        
        private class ProtocolSettings : IProtocolSettings
        {
            private uint Magic { get; }
            private byte AddressVersion { get; }
            private string[] SeedList { get; }

            protected internal ProtocolSettings(IConfiguration section)
            {
                Magic = uint.Parse(section.GetSection("Magic").Value);
                AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
                SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
            }
        }
    }
}