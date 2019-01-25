using System;

namespace Catalyst.Node.Modules.Core.Consensus
{
    public class Consensus : IDisposable, IConsensus
    {
        private static Consensus Instance { get; set; }
        private static readonly object Mutex = new object();      
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        /// <returns></returns>
        public static Consensus GetInstance()
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Consensus();
                }
            return Instance;
        }
        
        public void Dispose()
        {
            
        }
    }
}