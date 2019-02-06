using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using Autofac.Core.Lifetime;
using Catalyst.Node.Core.Helpers;
using Catalyst.Node.Core.Helpers.Logger;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Helpers.RLP;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.Gossip;
using Catalyst.Node.Core.Modules.Ledger;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Node.Core.Modules.P2P;
using Dawn;
using Microsoft.Extensions.Configuration;
using IModuleRegistrar = Autofac.Core.Registration.IModuleRegistrar;

namespace Catalyst.Node.Core
{
    public sealed class KernelBuilder
    {
        private readonly ContainerBuilder _containerBuilder;
        private readonly List<Action<Kernel>> _moduleLoader;
        private readonly NodeOptions _nodeOptions;

        /// <summary>
        /// </summary>
        public KernelBuilder(NodeOptions nodeOptions)
        {
            _nodeOptions = nodeOptions;
            _moduleLoader = new List<Action<Kernel>>();

            // Set path to load assemblies from ** be-careful **
            AssemblyLoadContext.Default.Resolving += (context, assembly) =>
                                                         context.LoadFromAssemblyPath(Path.Combine(
                                                             Directory.GetCurrentDirectory(),
                                                             $"{assembly.Name}.dll"));

            // get builder
            _containerBuilder = new ContainerBuilder();

            // register our options object
            _containerBuilder.RegisterType<NodeOptions>();

            // register components from config file
            _containerBuilder.RegisterModule(new ConfigurationModule(new ConfigurationBuilder()
                                                                    .AddJsonFile(
                                                                         $"{nodeOptions.DataDir}/components.json")
                                                                    .Build()));

            //            // return a built container
            //            return builder.Build();   
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithDfsModule()
        {
            _moduleLoader.Add(n => n.DfsService = _containerBuilder.RegisterModule(new DfsModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithPeerModule()
        {
            _moduleLoader.Add(n => n.P2PService = _containerBuilder.RegisterModule(new PeerModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithContractModule()
        {
            _moduleLoader.Add(n => n.ContractService = _containerBuilder.RegisterModule(new ContractModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithMempoolModule()
        {
            _moduleLoader.Add(n => n.MempoolService = _containerBuilder.RegisterModule(new MempoolModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithLedgerModule()
        {
            _moduleLoader.Add(n => n.LedgerService = _containerBuilder.RegisterModule(new LedgerModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithGossipModule()
        {
            _moduleLoader.Add(n => n.GossipService = _containerBuilder.RegisterModule(new GossipModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithConsensusModule()
        {
            _moduleLoader.Add(n => n.ConsensusService = _containerBuilder.RegisterModule(new ConsensusModule()));
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public KernelBuilder WithWalletModule()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public KernelBuilder When(Func<bool> condition)
        {
            var result = condition.Invoke();

            if (!result)
            {
                var oldAction = _moduleLoader[_moduleLoader.Count - 1];
                _moduleLoader.Remove(oldAction);
            }

            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Kernel Build()
        {
            var kernel = Kernel.GetInstance(_nodeOptions, _containerBuilder);
            _moduleLoader.ForEach(ba => ba(kernel));

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
        public IModuleRegistrar ConsensusService;
        public IModuleRegistrar ContractService;

        public IModuleRegistrar DfsService;
        public IModuleRegistrar GossipService;
        public IModuleRegistrar LedgerService;
        public IModuleRegistrar MempoolService;
        public IModuleRegistrar P2PService;

        /// <summary>
        ///     Private kernel constructor.
        /// </summary>
        /// <param name="nodeOptions"></param>
        /// <param name="container"></param>
        private Kernel(NodeOptions nodeOptions, IContainer container)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            Guard.Argument(container, nameof(container)).NotNull();
            NodeOptions = nodeOptions;
            Container = container;
        }

        public IContainer Container { get; set; }
        public NodeOptions NodeOptions { get; set; }
        public PeerIdentifier NodeIdentity { get; set; }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Container?.Dispose();
        }

        /// <summary>
        ///     Get a thread safe kernel singleton.
        /// </summary>
        /// <returns></returns>
        public static Kernel GetInstance(NodeOptions nodeOptions, ContainerBuilder containerBuilder)
        {
            Guard.Argument(nodeOptions, nameof(nodeOptions)).NotNull();
            if (_instance == null)
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
                            _instance = new Kernel(nodeOptions, containerBuilder.Build());
                        }
                        catch (Exception e)
                        {
                            LogException.Message("new kernel", e);
                            throw;
                        }
                    }
                }

            return _instance;
        }

        /// <summary>
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
                    Fs.CopySkeletonConfigs(nodeOptions.DataDir,
                        Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network));
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
                if (!Fs.CheckConfigExists(nodeOptions.DataDir,
                        Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network)))
                    try
                    {
                        // make config with new system folder
                        Fs.CopySkeletonConfigs(nodeOptions.DataDir,
                            Enum.GetName(typeof(NodeOptions.Networks), nodeOptions.Network));
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

        /// <summary>
        /// </summary>
        public void Shutdown()
        {
            throw new NotImplementedException();
            // @TODO some clean up and disposing of everything in some nice way
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Dispose(object sender, LifetimeScopeEndingEventArgs e)
        {
            Dispose();
        }
    }
}