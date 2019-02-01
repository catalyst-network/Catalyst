using System;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Contract
{
    public class Contract : IDisposable, IContract
    {
        private static Contract Instance { get; set; }
        private static readonly object Mutex = new object();      
        
        public Guid Identity { get; set; }
        public string Address { get; set; }
        public ContractType Type { get; set; }
        public StorageType StorageMedium { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        /// <returns></returns>
        public static Contract GetInstance()
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Contract();
                }
            return Instance;
        }

        public void Dispose()
        {
            
        }
    }
}
