using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Catalyst.Helpers.FileSystem;
using Catalyst.Helpers.Logger;
using Catalyst.Helpers.Platform;
using Catalyst.Helpers.RLP;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.Consensus;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.Mempool;
using Catalyst.Node.Modules.Core.P2P;
using Catalyst.Node.Modules.Core.Rpc;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node
{
    public sealed class Kernel //@TODO make disposable
    {
        private static Kernel _instance;
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Private kernel constructor.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="container"></param>
        private Kernel(Settings settings, IContainer container)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(container, nameof(container));
            Settings = settings;
            Container = container;
        }

        public IContainer Container { get; set; }
        private static Settings Settings { get; set; }

        /// <summary>
        ///     Get a thread safe kernel singleton.
        ///     @TODO need check that if we dont pass all module options you cant start a livenet instance
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions options)
        {
            Guard.NotNull(options, nameof(options));
            if (_instance == null)
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
                        else
                        {
                            // dir does exist, check config exits
                            if (!Fs.CheckConfigExists(options.DataDir, options.Network))
                                Fs.CopySkeletonConfigs(options.DataDir, options.Network);
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
                        
                        Guard.NotNull(settingsInstance, nameof(settingsInstance), "Couldn't instantiate settings class");

                        if (options.WalletRpcIp != null)
                        {
                            if (options.WalletRpcPort == 0)
                            {
                                options.WalletRpcPort =
                                    42444; // @TODO this isn't right we should override the setting file with cli options && it shouldn't be hard coded
                            }
                            else
                            {
                                // if we're not supplied a wallet check we have payout address and public key
                                if (options.WalletRpcIp == null && options.PayoutAddress == null ||
                                    options.WalletRpcIp == null && options.PublicKey == null)
                                    throw new Exception(
                                        "Need a wallet to connect to or be supplied public key and payout addresses");
                            }
                        }
                        else
                        {
                            // if we're not supplied a wallet check we have payout address and public key
                            if (options.WalletRpcIp == null && options.PayoutAddress == null ||
                                options.WalletRpcIp == null && options.PublicKey == null)
                                throw new Exception(
                                    "Need a wallet to connect to or be supplied public key and payout addresses");
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
                        Console.WriteLine(Path.Combine(Directory.GetCurrentDirectory()));

                        AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) => context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));

                        var componentConfig = new ConfigurationBuilder()
                            .AddJsonFile(options.DataDir + "/components.json")
                            .Build(); //@TODO try catch

                        var iocComponents = new ConfigurationModule(componentConfig);  //@TODO try catch
                        var builder = new ContainerBuilder();
                        builder.RegisterModule(iocComponents); //@TODO try catch

                        DfsModule.Load(builder, settingsInstance?.Dfs);
                        RpcModule.Load(builder, settingsInstance?.Rpc);
                        GossipModule.Load(builder, settingsInstance?.Gossip);
                        LedgerModule.Load(builder, settingsInstance?.Ledger);
                        MempoolModule.Load(builder, settingsInstance?.Mempool);
                        ContractModule.Load(builder, settingsInstance?.Contract);
                        ConsensusModule.Load(builder, settingsInstance?.Consensus);
                        P2PModule.Load(builder, settingsInstance?.Peer, options.DataDir,
                            options.PublicKey.ToBytesForRlpEncoding());
                        
                        Log.Message("=========");
                        Console.WriteLine(Type.GetType("Catalyst.Node.CatalystNode, Catalyst.Node"));
                        Console.WriteLine(Type.GetType("Catalyst.Node.Kernel, Catalyst.Node"));
                        Console.WriteLine(Type.GetType("Catalyst.Node.Modules.Core.Mempool.Mempool, Catalyst.Node.Modules.Core.Mempool"));
                        Log.Message("=========");
                        
                        var container = builder.Build(); //@TODO try catch

                        _instance = new Kernel(settingsInstance, container); //@TODO try catch
                    }
                }

            return _instance;
        }
    }
}