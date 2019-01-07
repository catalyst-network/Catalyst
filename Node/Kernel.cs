using System;
using Autofac;
using System.IO;
using ADL.Platform;
using System.Reflection;
using System.Runtime.Loader;
using Autofac.Configuration;
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
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (_instance == null) 
            { 
                lock (Mutex)
                {
                    if (_instance == null) 
                    { 
                        // run platform detection
                        options.Platform = Detection.Os();
            
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
                            Console.WriteLine("Boot process failed: {0}", e);
                        }
                        
                        if (settingsInstance == null)
                        {
                            throw new Exception("Couldn't instantiate settings class");    
                        }
                        
                        if (options.WalletRpcIp != null)
                        {
                            if (options.WalletRpcPort == 0)
                            {
                                options.WalletRpcPort = 42444;
                            }
                            else
                            {
                                // if we're not supplied a wallet check we have payout address and public key
                                if (options.WalletRpcIp == null && options.PayoutAddress == null || options.WalletRpcIp == null && options.PublicKey == null)
                                {
                                    throw new Exception("Need a wallet to connect to or be supplied public key and payout addresses");   
                                }
                            }
                        }
                        else {
                            // if we're not supplied a wallet check we have payout address and public key
                            if (options.WalletRpcIp == null && options.PayoutAddress == null || options.WalletRpcIp == null && options.PublicKey == null)
                            {
                                throw new Exception("Need a wallet to connect to or be supplied public key and payout addresses");   
                            }
                        }
                        
                        // check we have a pfx cert for watson server
//                        if (File.Exists(options.DataDir+"/"+settingsInstance.Ssl.PfxFileName) == false)
//                        {
//                            Console.WriteLine("===============");
//                            Console.WriteLine(settingsInstance.Ssl.SslCertPassword);
//                            Console.WriteLine("===============");
//                            X509Certificate x509Cert = SSLUtil.CreateCertificate(settingsInstance.Ssl.SslCertPassword, "node-name"); //@TODO get node name from somewhere
//                            SSLUtil.WriteCertificateFile(new DirectoryInfo(options.DataDir),  settingsInstance.Ssl.PfxFileName, x509Cert.Export(X509ContentType.Pfx));   
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

                        new DfsModule().Load(builder, settingsInstance.Dfs);
                        new RpcModule().Load(builder, settingsInstance.Rpc);
                        new GossipModule().Load(builder, settingsInstance.Gossip);
                        new LedgerModule().Load(builder, settingsInstance.Ledger);
                        new MempoolModule().Load(builder, settingsInstance.Mempool);
                        new ContractModule().Load(builder, settingsInstance.Contract);
                        new ConsensusModule().Load(builder, settingsInstance.Consensus);
                        new NetworkModule().Load(builder, settingsInstance.Peer, settingsInstance.Ssl, options);
                        
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
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (container == null) throw new ArgumentNullException(nameof(container));
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
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            return Directory.Exists(dataDir);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        private static void CreateSystemFolder(string dataDir)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
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
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (network == null) throw new ArgumentNullException(nameof(network));
            File.Copy(AppDomain.CurrentDomain.BaseDirectory +"/Config/components.json", dataDir);
            File.Copy(AppDomain.CurrentDomain.BaseDirectory + "/Config/"+network+".json", dataDir);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataDir"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        private static bool CheckConfigExists(string dataDir, string network)
        {
            if (dataDir == null) throw new ArgumentNullException(nameof(dataDir));
            if (network == null) throw new ArgumentNullException(nameof(network));
            return File.Exists(dataDir + "/"+network+".json");
        }
    }
}
