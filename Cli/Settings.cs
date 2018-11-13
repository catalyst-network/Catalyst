using System;
using Autofac;
using ADL.Cli.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ADL.Cli
{
    internal class Settings : INodeConfiguration
    {
        public uint Magic { get; private set; }
        public byte AddressVersion { get; private set; }
        public string[] SeedList { get; private set; }
        public static Settings Default { get; private set; }
        public string ChainPath { get; }
        public string ChainIndex { get; }
        public int P2PPort { get; }
        public int RPCAddress { get; }
        public int RPCPort { get; }
        
        static Settings()
        {
//            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("configs/protocol.json").Build().GetSection("ProtocolConfiguration");
            
//            Default = new Settings(section);
        }
        
//        public Settings(IConfigurationSection section)
//        {
//            this.Magic = uint.Parse(section.GetSection("Magic").Value);
//            this.AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
//            this.SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
//        }

    }
    
}