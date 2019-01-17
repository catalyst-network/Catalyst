using System;
using Autofac;

namespace Catalyst.Node.Modules.Core.P2P
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class P2PModule : AsyncModuleBase, IP2PModule
    {
        private string DataDir { get; set; }
        private byte[] PublicKey { get; set; }
        public P2PNetwork P2PNetwork { get; set; }
        private IP2PSettings P2PSettings { get; set; }

        public static ContainerBuilder Load(
            ContainerBuilder builder,
            IP2PSettings P2PSettings,
            string dataDir,
            byte[] publicKey
        )
        {
            //@TODO guard util
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (P2PSettings == null) throw new ArgumentNullException(nameof(P2PSettings));

            builder.Register(c => new P2PModule(P2PSettings, dataDir, publicKey))
                .As<IP2PModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="peer"></param>
        /// <param name="ip2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="options"></param>
        public P2PModule(IP2PSettings p2PSettings, string dataDir, byte[] publicKey)
        {
            //@TODO guard util
            DataDir = dataDir;
            PublicKey = publicKey;
            P2PSettings = p2PSettings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            P2PNetwork = P2PNetwork.GetInstance(P2PSettings, DataDir, PublicKey);
//            Catalyst.Helpers.Network.PeerManager.BuildOutBoundConnection("127.0.0.1", 42069);
            return true;
        }
            
        public override bool StopService()
        {
            P2PNetwork.Dispose();
            return false;
        }
        
        public IDht GetImpl()
        {
            return P2PNetwork;
        }
    }
} 
