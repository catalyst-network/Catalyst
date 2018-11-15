using System;
using Akka.Actor;

namespace ADL.RpcServer
{
    public class MempoolService : UntypedActor, IRpcServerService
    {
        protected override void PreStart() => Console.WriteLine("Started MempoolService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped MempoolService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("MempoolService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
