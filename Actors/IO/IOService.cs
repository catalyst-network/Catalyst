using System;
using Akka.Actor;

namespace ADL.IOService
{
    public class IOService : UntypedActor, IRpcServerService
    {
        protected override void PreStart() => Console.WriteLine("Started IOService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped IOService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("IOService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
