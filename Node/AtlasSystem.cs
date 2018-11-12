using System;
using ADL.Consensus;
using ADL.DFS;
using ADL.Ledger;
using ADL.LocalPeer;
using Akka;
using Akka.Actor;
using Autofac;
using Akka.DI.Core;
using Akka.DI.AutoFac;
using ADL.RpcServer;
using ADL.TaskManager;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable
    {
        private ActorSystem _actorSystem;
        
        public AtlasSystem()
        {
            RegisterServices();
        }
        
        private void RegisterServices()
        {
            Console.WriteLine("RegisterServices trace");

            using (_actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("AtlasSystem create trace");

                var container = BuildContainer(_actorSystem);

                var rpcActor = _actorSystem.ActorOf(container.Create<RpcServerService>(), "RpcServerService");
                var taskManagerActor = _actorSystem.ActorOf(container.Create<TaskManagerService>(), "TaskManagerService");
                var peerActor = _actorSystem.ActorOf(container.Create<LocalPeerService>(), "LocalPeerService");
                var ledgerActor = _actorSystem.ActorOf(container.Create<LedgerService>(), "LedgerService");
                var dfsActor = _actorSystem.ActorOf(container.Create<DFSService>(), "DFSService");
                var consensusActor = _actorSystem.ActorOf(container.Create<ConsensusService>(), "ConsensusService");

                rpcActor.Tell("im a rpcActor message");
                taskManagerActor.Tell("im a TaskManagerActor message");
                peerActor.Tell("im a peerActor message");
                ledgerActor.Tell("im a LedgerActor message");
                dfsActor.Tell("im a DFSActor message");
                consensusActor.Tell("im a consensusActor message");
                Console.WriteLine();
            }
        }
        
        private IDependencyResolver BuildContainer(ActorSystem actorSystem)
        {
            Console.WriteLine("BuildContainer trace");

            var builder = new ContainerBuilder();
            
            builder.RegisterType<RpcServerService>();
            builder.RegisterType<TaskManagerService>();
            builder.RegisterType<LocalPeerService>();
            builder.RegisterType<LedgerService>();
            builder.RegisterType<DFSService>();
            builder.RegisterType<ConsensusService>();

            IContainer container = builder.Build();
            return new AutoFacDependencyResolver(container, actorSystem);
        }
        
        public void Dispose()
        {
//            RpcServer?.Dispose();
//            ActorSystem.Stop(LocalNode);
            _actorSystem.Dispose();
        }

    }
}