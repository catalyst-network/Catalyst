using System;
using Akka.Actor;

namespace ADL.Gossip
{
    public class Gossip : UntypedActor, IGossip
    {
        protected override void PreStart() => Console.WriteLine("Started Gossip actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped Gossip actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("Gossip OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
