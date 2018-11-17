using System;
using System.IO;
using ADL.Node.Interfaces;
using Akka.Actor;
using Autofac;
using Autofac.Configuration;
using Akka.DI.AutoFac;
using ADL.Rpc.Server;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Runtime.Loader;
using ADL.Consensus;
using ADL.Contract;
using ADL.DFS;
using ADL.Gossip;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
    {        
        private ActorSystem _actorSystem;
        
        public IKernel Kernel { get; set; }
        
        public IActorRef ConsensusService { get; set; }

        public IActorRef GossipService { get; set; }

        public IRpcServer RcpService { get; set; }
        
        public IDFS DfsService { get; set; }

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
            
            builder.RegisterType<ContractService>().As<ContractService>();
            builder.RegisterType<ConsensusService>().As<ConsensusService>();
            builder.RegisterType<GossipService>().As<GossipService>();

            var container = builder.Build();
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            
            return new Kernel(resolver, settings, container);
        }
        
        public void StartConsensus()
        {
            Console.WriteLine("Consensus starting....");
            ConsensusService = _actorSystem.ActorOf(Kernel.Resolver.Create<ConsensusService>(), "ConsensusService");
        }
        
        public void StartGossip()
        {
            Console.WriteLine("Node starting....");
            GossipService = _actorSystem.ActorOf(Kernel.Resolver.Create<GossipService>(), "GossipService");
        }
        
        public void StartRcp()
        {
            Console.WriteLine("RPC should start");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                RcpService = scope.Resolve<IRpcServer>();
            }
        }
        
        public void StartDfs()
        {
            Console.WriteLine("DFS server starting....");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                DfsService = scope.Resolve<IDFS>();
            }
        }
        
        public void Dispose()
        {
            RcpService.Dispose();
            _actorSystem.Stop(ConsensusService);
            _actorSystem.Stop(GossipService);
            _actorSystem.Dispose();
        }
    }
}
