using System;
using Akka.Actor;

namespace ADL.DFS
{
    public class DFSService : TypedActor, IDFSService
    {
        protected override void PreStart() => Console.WriteLine("Started DFSService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped DFSService actor");
    
        protected void OnReceive(object message)
        {
            Console.WriteLine("DFSService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
