using System;
using Akka.Actor;

namespace ADL.DFS
{
    public class DFSService : UntypedActor, IDFSService
    {
        protected override void PreStart() => Console.WriteLine("Started DFSService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped DFSService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("DFSService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
