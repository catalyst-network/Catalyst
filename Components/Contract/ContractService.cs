using System;
using Akka.Actor;

namespace ADL.Contract
{
    public enum ContractType { Standard, External }
    
    public enum StorageType { ADfs, IPFS, MaidSafe, TrustedServer }
    
    public class ContractService : UntypedActor, IContract
    {
        public Guid Identity { get; set; }

        public ContractType Type { get; set; }
        
        public string Address { get; set; }
        
        public StorageType StorageMedium { get; set; }
        
        protected override void PreStart() => Console.WriteLine("Started DfsService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped DfsService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("DfsService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
