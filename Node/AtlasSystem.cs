using System;
using System.Collections.Generic;
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
using ADL.DFS;

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
//            
            builder.RegisterModule(configModule);

//            builder.RegisterMicrosoftConfigurationProvider(Settings);
//            builder.RegisterMicrosoftConfigurationProvider<Settings>().As<INodeConfiguration>();
            
//            builder.RegisterType<RpcServer>().As<RpcServer>();
//            builder.RegisterType<LocalPeerService>().As<LocalPeerService>();
//            builder.RegisterType<LedgerService>().As<LedgerService>();
//            builder.RegisterType<DFSService>().As<DFSService>();
//            builder.RegisterType<ConsensusService>().As<ConsensusService>().InstancePerLifetimeScope();

            var container = builder.Build();
            
            var resolver = new AutoFacDependencyResolver(container, actorSystem);
            
//            var rpcActor = _actorSystem.ActorOf(resolver.Create<RpcServerService>(), "RpcServerService");
//            var taskManagerActor = _actorSystem.ActorOf(resolver.Create<TaskManagerService>(), "TaskManagerService");
//            var peerActor = _actorSystem.ActorOf(resolver.Create<LocalPeerService>(), "LocalPeerService");
//            var ledgerActor = _actorSystem.ActorOf(resolver.Create<LedgerService>(), "LedgerService");
//            var dfsActor = _actorSystem.ActorOf(resolver.Create<DFSService>(), "DFSService");
//            var consensusActor = _actorSystem.ActorOf(resolver.Create<ConsensusService>(), "ConsensusService");

            return new Kernel(resolver, settings, container);
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
            Console.WriteLine("RPC should start");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                var plugin = scope.Resolve<IRpcServer>();
            }
        }
        
        public void StartDfs()
        {
//            Console.WriteLine("DFS server starting....");
//            using (var scope = Kernel.Container.BeginLifetimeScope())
//            {
//                var plugin = scope.Resolve<IDFS>();
//            }
        }
        
        public void Dispose()
        {
//            Rpc?.Dispose();
//            ActorSystem.Stop(LocalNode);
//            _actorSystem.Dispose();
        }
    }
}