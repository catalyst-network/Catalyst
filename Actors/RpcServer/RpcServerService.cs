using System;
using Akka;
using Akka.Actor;
using Akka.Remote;

namespace ADL.RpcServer
{
    public class RpcServerService : UntypedActor, IRpcServerService
    {
        protected override void PreStart() => Console.WriteLine("Started actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("123");

            Console.WriteLine($"Message received {message}");
        }
    }
}
