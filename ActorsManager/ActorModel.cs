using System;
using Akka.Actor;
using ADL.DFS;
using ADL.RpcServer;

namespace ADL.ActorManager
{
    public static class ActorModel
    {
        public static ActorSystem ADLActorSystem { get; set; }
        
        public static IActorRef RpcServerActorRef { get; set; }
        public static IActorRef DFSActorRef { get; set; }
        
        public static void StartActorSystem()
        {
            ADLActorSystem = ActorSystem.Create("ADLActorSystem");
            
            DFSActorRef = ADLActorSystem.ActorOf(DFSActor.Props, "DFSActor");
            RpcServerActorRef = ADLActorSystem.ActorOf(RpcServerActor.Props, "RpcServerActor");
        }
    }
}