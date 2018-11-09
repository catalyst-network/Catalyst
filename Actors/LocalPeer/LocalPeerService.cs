using System;
using Akka.Actor;

namespace ADL.LocalPeer
{
    public class LocalPeerService : UntypedActor, ILocalPeerService
    {
        protected override void PreStart() => Console.WriteLine("Started LocalPeerService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped LocalPeerService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("LocalPeerService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
