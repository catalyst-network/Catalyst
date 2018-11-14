using System.Net;
using ADL.Cli.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ADL.Cli
{
    internal class Settings : INodeConfiguration
    {
        public uint Magic { get; private set; }
        public byte AddressVersion { get; private set; }
        public string[] SeedList { get; private set; }
        public IPathSettings Paths { get; }
        public IP2PSettings P2P { get; }
        public IRPCSettings RPC { get; }
        
        public static Settings Default { get; private set; }

        static Settings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("Configs/protocol.json").Build().GetSection("ProtocolConfiguration");
            
            Default = new Settings(section);
        }
        
        protected internal Settings(IConfiguration section)
        {
            Paths = new PathSettings(section.GetSection("Paths"));
            P2P = new P2PSettings(section.GetSection("P2P"));
            RPC = new RpcSettings(section.GetSection("RPC"));
            this.Magic = uint.Parse(section.GetSection("Magic").Value);
            this.AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
        }
       
        private class PathSettings : IPathSettings
        {
            private string Chain { get; set; }
            private string Index { get; set; }

            protected internal PathSettings(IConfiguration section)
            {
                Chain = string.Format(section.GetSection("Chain").Value);
                Index = string.Format(section.GetSection("Index").Value);
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
    }
}