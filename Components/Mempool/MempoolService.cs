using System;
using Akka.Actor;

namespace ADL.Mempool
{
    public class MempoolService : UntypedActor, IMempool
    {
        protected override void PreStart() => Console.WriteLine("Started Mempool actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped Mempool actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("Mempool OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
