using System;
using System.IO;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Catalyst.Helpers.FileSystem;
using Catalyst.Helpers.Ipfs;
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
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node
{
    public sealed class Kernel : IDisposable
    {
        private static Kernel _instance;
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Private kernel constructor.
        /// </summary>
        /// <param name="nodeOptions"></param>
        /// <param name="container"></param>
        private Kernel(NodeOptions nodeOptions, IContainer container)
        {
            Guard.NotNull(nodeOptions, nameof(nodeOptions));
            Guard.NotNull(container, nameof(container));
            NodeOptions = nodeOptions;
            Container = container;
        }

        public IContainer Container { get; set; }
        private static NodeOptions NodeOptions { get; set; }

        /// <summary>
        ///     Get a thread safe kernel singleton.
        ///     @TODO need check that if we dont pass all module options you cant start a livenet instance
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions nodeOptions)
        {
            Guard.NotNull(nodeOptions, nameof(nodeOptions));
            if (_instance == null)
                lock (Mutex)
                {
                    if (_instance == null)
                    {
                        // check supplied data dir exists
                        if (!Fs.DataDirCheck(nodeOptions.DataDir))
                        {
                            // not there make one
                            Fs.CreateSystemFolder(nodeOptions.DataDir);
                            // make config with new system folder
                            Fs.CopySkeletonConfigs(nodeOptions.DataDir, nodeOptions.Network);
                        }
                        else
                        {
                            // dir does exist, check config exits
                            if (!Fs.CheckConfigExists(nodeOptions.DataDir, nodeOptions.Network))
                                Fs.CopySkeletonConfigs(nodeOptions.DataDir, nodeOptions.Network);
                        }
                        
                        _instance = new Kernel(nodeOptions, ConfigureServices(nodeOptions)); //@TODO try catch
                    }
                }
            return _instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IContainer ConfigureServices(NodeOptions options)
        {
            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(),
                    $"{assembly.Name}.dll"));

            var componentConfig = new ConfigurationBuilder()
                .AddJsonFile(options.DataDir + "/components.json")
                .GetInstance();
            
            var iocComponents = new ConfigurationModule(componentConfig); //@TODO try catch
            var builder = new ContainerBuilder();
            builder.RegisterModule(iocComponents); //@TODO try catch
            
            builder.Register(c => new DfsModule(c.Resolve<IIpfs>(), settingsInstance?.Dfs))
                .As<IDfsModule>()
                .InstancePerLifetimeScope();
                        
            builder.Register(c => new GossipModule(settingsInstance?.Gossip))
                .As<IGossipModule>()
                .InstancePerLifetimeScope();
                        
            builder.Register(c => new LedgerModule(c.Resolve<ILedger>(), settingsInstance?.Ledger))
                .As<ILedgerService>()
                .InstancePerLifetimeScope();
            builder.Register(c => new MempoolModule(c.Resolve<IMempool>(), settingsInstance?.Mempool))
                .As<IMempoolModule>()
                .InstancePerLifetimeScope();

            builder.Register(c => new ContractModule(c.Resolve<IContract>(), settingsInstance?.Contract))
                .As<IContractModule>()
                .InstancePerLifetimeScope();
                        
            builder.Register(c => new ConsensusModule(c.Resolve<IConsensus>(), settingsInstance?.Consensus))
                .As<IConsensusModule>()
                .InstancePerLifetimeScope();
                        
            builder.Register(c => new P2PModule(settingsInstance?.Peer, options.DataDir,
                    options.PublicKey.ToBytesForRlpEncoding()))
                .As<IP2PModule>()
                .InstancePerLifetimeScope();

            var container = builder.Build(); //@TODO try catch   

            return container;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Container?.Dispose();
        }
    }
}