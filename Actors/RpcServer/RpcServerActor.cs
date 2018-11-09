using System;
using Akka.Actor;

namespace ADL.RpcServer
{
    public class RpcServerActor : UntypedActor
    {
        public static Props Props => Props.Create(() => new RpcServerActor());
        
        protected override void PreStart() => Console.WriteLine("Started actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine($"Message received {message}");
        }
    }
}
