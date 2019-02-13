using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using Autofac;
using Autofac.Configuration;
using AutofacSerilogIntegration;
using Catalyst.Node.Core.Helpers.Network;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Contract;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.Modules.Gossip;
using Catalyst.Node.Core.Modules.Ledger;
using Catalyst.Node.Core.Modules.Mempool;
using Catalyst.Node.Core.P2P;
using Microsoft.Extensions.Configuration;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Ioc.Autofac;

namespace Catalyst.Node.Core {
    public sealed class KernelBuilder
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            
            // register our persistence repository implementations
            _containerBuilder.RegisterSharpRepository(_nodeOptions.PersistenceConfiguration);
            
            // register our options object
            _containerBuilder.RegisterType<NodeOptions>();

            // register components from config file
            _containerBuilder.RegisterModule(new ConfigurationModule(new ConfigurationBuilder()
                                                                    .AddJsonFile(Path.Combine(nodeOptions.DataDir, "components.json"))
                                                                    .Build()));

            Serilog.Log.Logger = new LoggerConfiguration()
                                .WriteTo.Console()
                                .CreateLogger();

            _containerBuilder.RegisterLogger();
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
                    kernel.NodeOptions.PeerSettings.PublicKey.ToBytesForRLPEncoding(),
                    EndpointBuilder.BuildNewEndPoint(
                        kernel.NodeOptions.PeerSettings.BindAddress,
                        kernel.NodeOptions.PeerSettings.Port
                    )
                );
            }
            catch (ArgumentNullException e)
            {
                Logger.Error(e, "Failed to build Kernel");
                throw;
            }

            return kernel;
        }
    }
}