using ADL.P2P;
using ADL.Rpc;
using ADL.DFS;
using ADL.Gossip;
using ADL.Mempool;
using ADL.Contract;
using ADL.Consensus;
using System;
using Autofac;
using System.IO;
using Akka.Actor;
using Akka.DI.AutoFac;
using System.Reflection;
using ADL.Node.Interfaces;
using Autofac.Configuration;
using System.Runtime.Loader;
using ADL.Node.Ledger;
using Microsoft.Extensions.Configuration;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
    {        
        private ActorSystem _actorSystem;
                
        public IActorRef ContractSystem { get; set; }
        
        public IADL ADLedger { get; set; }

        public IDFS DfsService { get; set; }
        
        public IP2P P2PService { get; set; }

        public IRpcService RcpService { get; set; }
        
        internal IKernel Kernel { get; set; }

        public AtlasSystem()
        {         
            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");
                Kernel = BuildKernel(_actorSystem, Settings.Default.Sections);
            }
        }
       
        /// <summary>
        /// Registers all services on IOC container and returns an application kernel.
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static IKernel BuildKernel(ActorSystem actorSystem, INodeConfiguration settings)
        {
            Console.WriteLine("BuildContainer trace");
            
            var builder = new ContainerBuilder();

            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
            {
                return context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));
            };
            
            var config = new ConfigurationBuilder()
                .AddJsonFile(Directory.GetCurrentDirectory()+"/../Node/Configs/components.json")
                .Build();
            var configModule = new ConfigurationModule(config);

            builder.RegisterModule(configModule);
            
            builder.RegisterType<ConsensusService>().As<ConsensusService>();
            builder.RegisterType<ContractService>().As<ContractService>();
            builder.RegisterType<GossipService>().As<GossipService>();

            var container = builder.Build();
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            
            return new Kernel(resolver, settings, container);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StartRpc()
        {
            Console.WriteLine("RPC should start");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                RcpService = scope.Resolve<IRpcService>();
            }
            RcpService.StartServer(Kernel.Settings.Rpc);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StopRpc()
        {
            RcpService.StopServer();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StartDfs()
        {
            Console.WriteLine("Dfs server starting....");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                DfsService = scope.Resolve<IDFS>();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StartConsensus()
        {
            Console.WriteLine("Consensus starting....");
            ADLedger.ConsensusService = _actorSystem.ActorOf(Kernel.Resolver.Create<ConsensusService>(), "ConsensusService");
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StartGossip()
        {
            Console.WriteLine("Node starting....");
            ADLedger.GossipService = _actorSystem.ActorOf(Kernel.Resolver.Create<GossipService>(), "GossipService");
        }
        
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Dispose()
        {
            RcpService?.StopServer();
//            _actorSystem.Stop(ConsensusService);
//            _actorSystem.Stop(GossipService);
//            _actorSystem.Dispose();
        }
    }
}
