using Akka.Actor;
using ADL.DFS;
using ADL.RpcServer;

namespace ADL.ActorManager
{
    public static class ActorModel
    {
        private static ActorSystem AdlActorSystem { get; set; }
        
        public static IActorRef RpcServerActorRef { get; set; }
        public static IActorRef DfsActorRef { get; set; }
        
        public static void StartActorSystem()
        {
            AdlActorSystem = ActorSystem.Create("ADLActorSystem");

            DfsActorRef = AdlActorSystem.ActorOf<DfsActor>("DFSActor");            
            RpcServerActorRef = AdlActorSystem.ActorOf<RpcServerActor>("RpcServerActor");
        }
    }
}