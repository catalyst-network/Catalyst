using System;
using ADL.Node.Interfaces;
using Akka.Actor;
using Akka;
using Akka.Actor;
using Autofac;
using Autofac.Configuration;
using Akka.DI.Core;
using Akka.DI.AutoFac;
using ADL.Rpc.Server;
using Akka.Actor.Dsl;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;
using Microsoft.Extensions.Configuration;
using Thinktecture;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
    {        
        private static ActorSystem _actorSystem;
        
        public IKernel Kernel { get; set; }

        public AtlasSystem()
        {           
            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");
                Kernel = BuildKernel(_actorSystem, Settings.Default);
            }
        }
       
        /// <summary>
        /// Registers all services on IOC containers and returns an application kernel.
        /// </summary>
        /// <param name="actorSystem"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static IKernel BuildKernel(ActorSystem actorSystem, INodeConfiguration settings)
        {
            Console.WriteLine("BuildContainer trace");
            
            var builder = new ContainerBuilder();

//            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assembly) =>
//            {
//                return context.LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.dll"));
//            };
            
//            var config = new ConfigurationBuilder()
//                .AddJsonFile(Directory.GetCurrentDirectory()+"/Configs/services.json")
//                .Build();
//            var configModule = new ConfigurationModule(config);
//            
//            builder.RegisterModule(configModule);

//            builder.RegisterMicrosoftConfigurationProvider(Settings);
//            builder.RegisterMicrosoftConfigurationProvider<Settings>().As<INodeConfiguration>();
            
            builder.RegisterType<RpcServer>().As<RpcServer>();
//            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
//            builder.RegisterType<LedgerService>().As<LedgerService>();
//            builder.RegisterType<DFSService>().As<DFSService>();
//            builder.RegisterType<ConsensusService>().As<ConsensusService>().InstancePerLifetimeScope();

            var container = builder.Build();
            
            using (var scope = container.BeginLifetimeScope())
            {
//                var plugin = scope.Resolve<IDFSService>();
//                Console.WriteLine("Resolved specific plugin type: {0}");
//                Console.WriteLine("All available plugins:");
//                var allPlugins = scope.Resolve<IEnumerable<IDFSService>>();
//                foreach (var resolved in allPlugins)
//                {
//                    Console.WriteLine("- {0}");
//                }
            }
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            
//            var rpcActor = _actorSystem.ActorOf(resolver.Create<RpcServerService>(), "RpcServerService");
//            var taskManagerActor = _actorSystem.ActorOf(resolver.Create<TaskManagerService>(), "TaskManagerService");
//            var peerActor = _actorSystem.ActorOf(resolver.Create<LocalPeerService>(), "LocalPeerService");
//            var ledgerActor = _actorSystem.ActorOf(resolver.Create<LedgerService>(), "LedgerService");
//            var dfsActor = _actorSystem.ActorOf(resolver.Create<DFSService>(), "DFSService");
//            var consensusActor = _actorSystem.ActorOf(resolver.Create<ConsensusService>(), "ConsensusService");

            return new Kernel(resolver, settings);
        }
        
        public void StartConsensus()
        {
            Console.WriteLine("Consensus starting....");
//            var consensusActor = _actorSystem.ActorOf(resolver.Create<ConsensusService>(), "ConsensusService");
        }
        
        public void StartNode()
        {
            Console.WriteLine("Node starting....");
//            var peerActor = _actorSystem.ActorOf(resolver.Create<LocalPeerService>(), "LocalPeerService");
        }
        
        public void StartRcp()
        {
            var rpcServer = new RpcServer();
        }
        
        public void StartDfs()
        {
            Console.WriteLine("DFS server starting....");
//            var dfsActor = _actorSystem.ActorOf(resolver.Create<DFSService>(), "DFSService");
        }
        
        public void Dispose()
        {
//            Rpc?.Dispose();
//            ActorSystem.Stop(LocalNode);
//            _actorSystem.Dispose();
        }
    }
}