using System;
using Akka.Actor;

namespace ADL.RpcServer
{
    public class RpcServerService : UntypedActor, IRpcServerService
    {
        protected override void PreStart() => Console.WriteLine("Started RpcServerService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped RpcServerService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("RpcServerService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
