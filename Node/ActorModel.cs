using Akka.Actor;
using ADL.RpcServer;

namespace ADL.ActorManager
{
    public static class ActorModel
    {
        private static ActorSystem AdlActorSystem { get; set; }
        public static IActorRef RpcServerActorRef { get; set; }
        
        public static void StartActorSystem()
        {
            AdlActorSystem = ActorSystem.Create("ADLActorSystem");       
            RpcServerActorRef = AdlActorSystem.ActorOf<RpcServerActor>("RpcServerActor");
        }
    }
}