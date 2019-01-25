using System;
using System.Threading.Tasks;

namespace Catalyst.Node.Modules.Core.Ledger
{
    public class Ledger : IDisposable, ILedger
    {
        private static Ledger Instance { get; set; }
        private static readonly object Mutex = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        /// <returns></returns>
        public static Ledger GetInstance()
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Ledger();
                }
            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
        }
    }
}
