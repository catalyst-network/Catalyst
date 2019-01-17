using System;
using Autofac;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autofac.Configuration;
using Catalyst.Helpers.FileSystem;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Platform;
using Catalyst.Helpers.RLP;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Rpc;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.P2P;
using Catalyst.Node.Modules.Core.Mempool;
using Microsoft.Extensions.Configuration;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Consensus;

namespace Catalyst.Node
{
    public sealed class Kernel//@TODO make disposable
    {
        private static Kernel _instance;
        public IContainer Container { get; set; }
        private static Settings Settings { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get a thread safe kernel singleton.
        /// @TODO need check that if we dont pass all module options you cant start a livenet instance
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions options)
        {
            //@TODO guard utils
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
                        if (!Fs.DataDirCheck(options.DataDir))
                        {
                            // not there make one
                            Fs.CreateSystemFolder(options.DataDir);
                            // make config with new system folder
                            Fs.CopySkeletonConfigs(options.DataDir, options.Network);
                        }
                        else {
                            // dir does exist, check config exits
                            if (!Fs.CheckConfigExists(options.DataDir, options.Network))
                            {
                                // doesn't, make one 
                                Fs.CopySkeletonConfigs(options.DataDir, options.Network); 
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
                            LogException.Message("Boot process failed: {0}", e);
                        }
                        
                        if (settingsInstance == null)
                        {
                            throw new Exception("Couldn't instantiate settings class");    
                        }
                        
                        if (options.WalletRpcIp != null)
                        {
                            if (options.WalletRpcPort == 0)
                            {
                                options.WalletRpcPort = 42444; // @TODO this isn't right we should override the setting file with cli options && it shouldn't be hard coded
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
//                            Log.Message("===============");
//                            Log.Message(settingsInstance.Ssl.SslCertPassword);
//                            Log.Message("===============");
//                            X509Certificate x509Cert = SSLUtil.CreateCertificate(settingsInstance.Ssl.SslCertPassword, "node-name"); //@TODO get node name from somewhere
//                            SSLUtil.WriteCertificateFile(new DirectoryInfo(options.DataDir),  settingsInstance.Ssl.PfxFileName, x509Cert.Export(X509ContentType.Pfx));   
//                        }
//                        else
//                        {
//                            Log.Message("got cert");                
//                        }
                        var builder = new ContainerBuilder();
            
                        AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
                            context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));

                        var componentConfig = new ConfigurationBuilder()
                            .AddJsonFile(options.DataDir+"/components.json")
                            .Build();
                        
                        var iocComponents = new ConfigurationModule(componentConfig);

                        builder.RegisterModule(iocComponents);
                        
                        builder = DfsModule.Load(builder, settingsInstance.Dfs);
                        builder = RpcModule.Load(builder, settingsInstance.Rpc);
                        builder = GossipModule.Load(builder, settingsInstance.Gossip);
                        builder = LedgerModule.Load(builder, settingsInstance.Ledger);
                        builder = MempoolModule.Load(builder, settingsInstance.Mempool);
                        builder = ContractModule.Load(builder, settingsInstance.Contract);
                        builder = ConsensusModule.Load(builder, settingsInstance.Consensus);
                        builder = P2PModule.Load(builder, settingsInstance.Peer, options.DataDir, options.PublicKey.ToBytesForRlpEncoding());   

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
        /// <param name="settings"></param>
        /// <param name="container"></param>
        private Kernel(Settings settings, IContainer container)
        {
            Guard.NotNull(settings,nameof(settings));
            Guard.NotNull(container,nameof(container));
            Settings = settings;
            Container = container;
        }
    }
}
