using Autofac;
using System;
using System.IO;
using ADL.Gossip;
using ADL.Contract;
using ADL.Platform;
using Akka.DI.Core;
using ADL.Consensus;
using System.Reflection;
using System.Runtime.Loader;
using Autofac.Configuration;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    public sealed class Kernel
    {
        private static Kernel _instance;
        public Settings Settings { get; set; }
        public IContainer Container { get; set; }
        public IDependencyResolver Resolver { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get a thread safe kernel singleton.
        /// </summary>
        /// <param name="resolver"></param>
        /// <param name="settings"></param>
        /// <param name="container"></param>
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
            
                        var iocConfig = new ConfigurationBuilder()
                            .AddJsonFile(options.DataDir+"/components.json")
                            .Build();
                        var iocConfigModule = new ConfigurationModule(iocConfig);

                        builder.RegisterModule(iocConfigModule);

                        // We can't resolve actors from config unless support added to underlying autofac library
                        // @TODO future improvement to load from autofac config json
                        builder.RegisterType<GossipService>().As<GossipService>();
                        builder.RegisterType<ContractService>().As<ContractService>();
                        builder.RegisterType<ConsensusService>().As<ConsensusService>();
            
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
