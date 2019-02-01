using System;
using System.Collections.Generic;
using Autofac;
using System.IO;
using System.Runtime.Loader;
using Autofac.Configuration;
using Autofac.Core.Lifetime;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Helpers.RLP;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.P2P;
using Dawn;
using Microsoft.Extensions.Configuration;

namespace Catalyst.Node.Core
{
    public sealed class KernelBuilder
    {
        public Kernel kernel;

        /// <summary>
        /// 
        /// </summary>
        public KernelBuilder(NodeOptions nodeOptions)
        {
            kernel = Kernel.GetInstance(nodeOptions);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithDfsModule()
        {
            kernel.ServiceLoader.Add(n => n.DfsService = kernel.Container.Resolve<IDfs>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithPeerModule()
        {
//            kernel.ServiceLoader.Add(n => n.P2PService = kernel.Container.Resolve<IP2P>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithContractModule()
        {
            kernel.ServiceLoader.Add(n => n.ContractService = kernel.Container.Resolve<IContract>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithMempoolModule()
        {
            kernel.ServiceLoader.Add(n => n.MempoolService = kernel.Container.Resolve<IMempool>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithLedgerModule()
        {
            kernel.ServiceLoader.Add(n => n.LedgerService = kernel.Container.Resolve<ILedger>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithGossipModule()
        {
            kernel.ServiceLoader.Add(n => n.GossipService = kernel.Container.Resolve<IGossip>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithConsensusModule()
        {
            kernel.ServiceLoader.Add(n => n.ConsensusService = kernel.Container.Resolve<IConsensus>());
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithWalletModule()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public KernelBuilder When(Func<Boolean> condition)
        {
            var result = condition.Invoke();

            if (!result)
            {
                var oldAction = kernel.ServiceLoader[kernel.ServiceLoader.Count - 1];
                kernel.ServiceLoader.Remove(oldAction);
            }

            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Kernel Build()
        {
            Log.ByteArr(kernel.NodeOptions.PeerSettings.PublicKey.ToBytesForRlpEncoding());
            Log.Message(kernel.NodeOptions.PeerSettings.BindAddress.ToString());
            Log.Message(kernel.NodeOptions.PeerSettings.Port.ToString());

            try
            {
                kernel.NodeIdentity = PeerIdentifier.BuildPeerId(
                    kernel.NodeOptions.PeerSettings.PublicKey.ToBytesForRlpEncoding(),
                    EndpointBuilder.BuildNewEndPoint(
                        kernel.NodeOptions.PeerSettings.BindAddress,
                        kernel.NodeOptions.PeerSettings.Port
                    )
                );
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("Catalyst.Helpers.Network GetInstance", e);
                throw;
            }
            return kernel;
        }
    }
    
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
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            Guard.Argument(container, nameof(container)).NotNull();
            ServiceLoader = new List<Action<Kernel>>();
            NodeOptions = nodeOptions;
            Container = container;
        }

        public IContainer Container { get; set; }
        public NodeOptions NodeOptions { get; set; }
        public PeerIdentifier NodeIdentity { get; set; }
        
        public IDfs DfsService;
//        public IP2P P2PService;
        public IGossip GossipService;
        public ILedger LedgerService;
        public IMempool MempoolService;
        public IContract ContractService;
        public IConsensus ConsensusService;

        internal readonly List<Action<Kernel>> ServiceLoader;

        /// <summary>
        ///     Get a thread safe kernel singleton.
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions nodeOptions)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            if (_instance == null)
            {
             lock (Mutex)
                {
                    if (_instance == null)
                    {
                        try
                        {
                            RunConfigStartUp(nodeOptions);
                        }
                        catch (Exception e)
                        {
                            LogException.Message("RunConfigStartUp", e);
                            throw;
                        }

                        try
                        {
                            _instance = new Kernel(nodeOptions, Configure(nodeOptions));   
                        }
                        catch (Exception e)
                        {
                            LogException.Message("new kernel", e);
                            throw;
                        }
                    }
                }   
            }
            return _instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeOptions"></param>
        /// <returns></returns>
        private static void RunConfigStartUp(NodeOptions nodeOptions)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            // check supplied data dir exists
            if (!Fs.DataDirCheck(nodeOptions.DataDir))
            {
                try
                {
                    // not there make one
                    Fs.CreateSystemFolder(nodeOptions.DataDir);
                }
                catch (ArgumentNullException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }
                catch (ArgumentException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }
                catch (IOException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }

                try
                {
                    // make config with new system folder
                    Fs.CopySkeletonConfigs(nodeOptions.DataDir, Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network));
                }
                catch (ArgumentNullException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }
                catch (ArgumentException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }
                catch (IOException e)
                {
                    LogException.Message(e.Message, e);
                    throw;
                }
            }
            else
            {
                // dir does exist, check config exits
                if (!Fs.CheckConfigExists(nodeOptions.DataDir, Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network)))
                {
                    try
                    {
                        // make config with new system folder
                        Fs.CopySkeletonConfigs(nodeOptions.DataDir, Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network));
                    }
                    catch (ArgumentNullException e)
                    {
                        LogException.Message(e.Message, e);
                        throw;
                    }
                    catch (ArgumentException e)
                    {
                        LogException.Message(e.Message, e);
                        throw;
                    }
                    catch (IOException e)
                    {
                        LogException.Message(e.Message, e);
                        throw;
                    }
                }
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        public CatalystNode StartUp()
        {
            CatalystNode catalystNode = CatalystNode.GetInstance(this);
            using (var kernelScope = catalystNode.Kernel.Container.BeginLifetimeScope())
            {
                kernelScope.CurrentScopeEnding += Dispose; //@TODO logic check this?
                catalystNode.Kernel.ServiceLoader.ForEach(ba => ba(this));
                return catalystNode;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Shutdown()
        {
            throw new NotImplementedException();
            // @TODO some clean up and disposing of everything in some nice way
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IContainer Configure(NodeOptions options)
        {
            // Set path to load assemblies from ** be-careful **
            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(),
                    $"{assembly.Name}.dll"));
            
            // get builder
            var builder = new ContainerBuilder();
            
            // register our options object
            builder.RegisterType<NodeOptions>();
            
            // register modules from config file
            var coreModules = new ConfigurationModule(new ConfigurationBuilder()
                .AddJsonFile($"{options.DataDir}/modules.json")
                .Build());
            builder.RegisterModule(coreModules);
                        
            // register components from config file
            var components = new ConfigurationModule(new ConfigurationBuilder()
                .AddJsonFile($"{options.DataDir}/components.json")
                .Build());
            builder.RegisterModule(components);

            // return a built container
            return builder.Build();   
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispose(object sender, LifetimeScopeEndingEventArgs e)
        {
            Dispose();
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