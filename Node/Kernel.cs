using System;
using Autofac;
using System.IO;
using ADL.Platform;
using System.Reflection;
using System.Runtime.Loader;
using ADL.DataStore;
using ADL.Node.Core.Modules.Consensus;
using ADL.Node.Core.Modules.Contract;
using ADL.Node.Core.Modules.Dfs;
using ADL.Node.Core.Modules.Gossip;
using ADL.Node.Core.Modules.Ledger;
using ADL.Node.Core.Modules.Mempool;
using ADL.Node.Core.Modules.Peer;
using ADL.Node.Core.Modules.Rpc;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    public sealed class Kernel
    {
        private static Kernel _instance;
        public IContainer Container { get; set; }
        public static Settings Settings { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get a thread safe kernel singleton.
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions options)
        { 
            if (_instance == null) 
            { 
                lock (Mutex)
                {
                    if (_instance == null) 
                    { 
                        // run platform detection
                        options.Platform = Detection.OS();
            
                        // check supplied data dir exists
                        if (!DataDirCheck(options.DataDir))
                        {
                            // not there make one
                            CreateSystemFolder(options.DataDir);
                            // make config with new system folder
                            CopySkeletonConfigs(options.DataDir, options.Network);
                        }
                        else {
                            // dir does exist, check config exits
                            if (!CheckConfigExists(options.DataDir, options.Network))
                            {
                                // doesn't, make one 
                                CopySkeletonConfigs(options.DataDir, options.Network); 
                            }
                        }
                        
                        // all configs should exist so attempt to load them into settings instance
                        Settings settingsInstance = null;
                        try
                        {
                            settingsInstance = Settings.GetInstance(options);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Boot process failed: {0}", e.ToString());
                        }
                        
                        if (settingsInstance == null)
                        {
                            throw new Exception("Couldn't instantiate settings class");    
                        }
                        
                        if (options.WalletRpcIp != null && options.WalletRpcPort == 0)
                        {
                            options.WalletRpcPort = 42444;
                        }
                        else {
                            // if we're not supplied a wallet check we have payout address and public key
                            if (options.WalletRpcIp == null && options.PayoutAddress == null || options.WalletRpcIp == null && options.PublicKey == null)
                            {
                                throw new Exception("Need a wallet to connect to or be supplied public key and payout addresses");   
                            }
                        }
                        // check we have a pfx cert for watson server
//                        if (File.Exists(settingsInstance.NodeConfiguration.NodeOptions.DataDir+"/"+settingsInstance.NodeConfiguration.Ssl.PfxFileName) == false)
//                        {
//                            X509Certificate x509Cert = SSLUtil.CreateCertificate(settingsInstance.NodeConfiguration.Ssl.SslCertPassword, "node-name"); //@TODO get node name from somewhere
//                            SSLUtil.WriteCertificateFile(new DirectoryInfo(settingsInstance.NodeConfiguration.NodeOptions.DataDir),  settingsInstance.NodeConfiguration.Ssl.PfxFileName, x509Cert.Export(X509ContentType.Pfx));   
//                        }
//                        else
//                        {
//                            Console.WriteLine("got cert");                
//                        }
                        var builder = new ContainerBuilder();
            
                        AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
                            context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));
            
                        var componentConfig = new ConfigurationBuilder()
                            .AddJsonFile(options.DataDir+"/components.json")
                            .Build();
                            
                        var iocComponents = new ConfigurationModule(componentConfig);

                        builder.RegisterModule(iocComponents);
                        
                        builder.Register(c => new ConsensusService(c.Resolve<IConsensus>(), settingsInstance.Consensus))
                            .As<IConsensusService>()
                            .InstancePerLifetimeScope();
                        
                        builder.Register(c => new ContractService(c.Resolve<IContract>(), settingsInstance.Contract))
                            .As<IContractService>()
                            .InstancePerLifetimeScope();
                        
//                        builder.Register(c => new DfsService(c.Resolve<IDfsService>(), settingsInstance.Dfs))
//                            .As<IDfsService>()
//                            .InstancePerLifetimeScope();

                        builder.Register(c => new GossipService(c.Resolve<IGossip>(), settingsInstance.Gossip))
                            .As<IGossipService>()
                            .InstancePerLifetimeScope();
                        
                        builder.Register(c => new LedgerService(c.Resolve<ILedger>(), settingsInstance.Ledger))
                            .As<ILedgerService>()
                            .InstancePerLifetimeScope();
                        
                        builder.Register(c => new MempoolService(c.Resolve<IMempool>(), settingsInstance.Mempool))
                            .As<IMempoolService>()
                            .InstancePerLifetimeScope();
                        
                        builder.Register(c => new PeerService(c.Resolve<IPeer>(), settingsInstance.Peer, options))
                            .As<IPeerService>()
                            .InstancePerLifetimeScope();
                        
                        builder.Register(c => new RpcService(c.Resolve<IRpcServer>(), settingsInstance.Rpc))
                            .As<IRpcService>()
                            .InstancePerLifetimeScope();
            
                        var container = builder.Build();
                        
                        _instance = new Kernel(settingsInstance, container);
                    }
                } 
            }
            return _instance;
        }
        
        /// <summary>
        /// Private kernel constructor.
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="settings"></param>
        /// <param name="container"></param>
        private Kernel(Settings settings, IContainer container)
        {
            Settings = settings;
            Container = container;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        private static bool DataDirCheck(string dataDir)
        {
            return Directory.Exists(dataDir);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        private static void CreateSystemFolder(string dataDir)
        {
            Directory.CreateDirectory(dataDir);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private static void CopySkeletonConfigs(string dataDir, string network)
        {
            File.Copy(AppDomain.CurrentDomain.BaseDirectory +"/config/components.json", dataDir);
            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/config/"+network+".json", dataDir);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private static bool CheckConfigExists(string dataDir, string network)
        {
            return File.Exists(dataDir + "/"+network+".json");
        }
    }
}
