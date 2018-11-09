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

namespace ADL.ADLNode
{
    public class AtlasService
    {
        public AtlasService()
        {
            RegisterServices();
        }
        
        private void RegisterServices()
        {
            Console.WriteLine("trace");

            using (var actorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Console.WriteLine("trace");

                var container = BuildContainer(actorSystem);

                var rpcActor = actorSystem.ActorOf(container.Create<RpcServerService>(), "RpcServerService");
                var TaskManagerActor = actorSystem.ActorOf(container.Create<TaskManagerService>(), "TaskManagerService");
                var peerActor = actorSystem.ActorOf(container.Create<LocalPeerService>(), "LocalPeerService");
                var LedgerActor = actorSystem.ActorOf(container.Create<LedgerService>(), "LedgerService");
                var DFSActor = actorSystem.ActorOf(container.Create<DFSService>(), "DFSService");
                var consensusActor = actorSystem.ActorOf(container.Create<ConsensusService>(), "ConsensusService");

                rpcActor.Tell("im a message");
                TaskManagerActor.Tell("im a message");
                peerActor.Tell("im a message");
                LedgerActor.Tell("im a message");
                DFSActor.Tell("im a message");
                consensusActor.Tell("im a message");
                Console.WriteLine();
            }
        }
        
        private static IDependencyResolver BuildContainer(ActorSystem actorSystem)
        {
            Console.WriteLine("trace");

            var builder = new ContainerBuilder();
            builder.RegisterType<RpcServerService>();
            IContainer container = builder.Build();
            return new AutoFacDependencyResolver(container, actorSystem);
        }
    }
}