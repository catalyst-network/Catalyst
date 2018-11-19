using System;
using Akka.Actor;

namespace ADL.Consensus
{
    public class ConsensusService : UntypedActor, IConsensus
    {
        protected override void PreStart() => Console.WriteLine("Started Consensus actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped Consensus actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("Consensus Actor OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
