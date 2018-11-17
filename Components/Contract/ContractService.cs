using System;
using Akka.Actor;

namespace ADL.Contract
{
    public enum ContractType { Standard, External }
    
    public enum StorageType { ADFS, IPFS, MaidSafe, TrustedServer }
    
    public class ContractService : UntypedActor, IContract
    {
        public Guid Identity { get; set; }

        public ContractType Type { get; set; }
        
        public string Address { get; set; }
        
        public StorageType StorageMedium { get; set; }
        
        protected override void PreStart() => Console.WriteLine("Started DFSService actor");
    
        protected override void PostStop() => Console.WriteLine("Stopped DFSService actor");
    
        protected override void OnReceive(object message)
        {
            Console.WriteLine("DFSService OnReceive");

            Console.WriteLine($"Message received {message}");
        }
    }
}
