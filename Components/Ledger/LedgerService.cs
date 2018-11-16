using System;
using Akka.Actor;

namespace ADL.Ledger
{
    public class LedgerService : UntypedActor, ILedger
    {
        protected override void PreStart() => Console.WriteLine("Started LedgerService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped LedgerService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("LedgerService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
