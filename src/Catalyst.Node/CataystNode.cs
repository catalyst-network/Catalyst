using System;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Modules.Core.Consensus;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.Mempool;
using Catalyst.Node.Modules.Core.P2P;
using Dawn;

namespace Catalyst.Node
{
    public class CatalystNode : IDisposable
    {
        private readonly Kernel Kernel;
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(Kernel kernel)
        {
            Kernel = kernel;
        }
        
        private static CatalystNode Instance { get; set; }

        /// <summary>
        ///     Get a thread safe CatalystSystem singleton.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CatalystNode GetInstance(Kernel kernel)
        {
            Guard.Argument(kernel, nameof(kernel)).NotNull();
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new CatalystNode(kernel);
                }
            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Kernel?.Dispose();
        }
    }
}