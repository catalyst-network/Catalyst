using System;
using Akka;
using Akka.Actor;
using Akka.Remote;

namespace ADL.RpcServer
{
    public class RpcServer : UntypedActor
    {
        protected override void PreStart() => Console.WriteLine("Started actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine($"Message received {message}");
        }
    }
}
