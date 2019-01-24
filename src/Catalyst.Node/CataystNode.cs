using System;
using System.Threading.Tasks;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.Consensus;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.Mempool;
using Catalyst.Node.Modules.Core.P2P;

namespace Catalyst.Node
{
    public class CatalystNode : IDisposable
    {
        private readonly Kernel kernel;
        private static readonly object Mutex = new object();

        /// <summary>
        ///     Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(Kernel kernel)
        {
            kernel = kernel;
        }
        
        private static CatalystNode Instance { get; set; }

        /// <summary>
        ///     Get mempool implementation (static)
        /// </summary>
        /// <returns>IMempoolService</returns>
        public static IMempoolModule MempoolModule { get; private set; }

        /// <summary>
        ///     Get a thread safe CatalystSystem singleton.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CatalystNode GetInstance(Kernel kernel)
        {
            Guard.NotNull(kernel, nameof(kernel));
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new CatalystNode(kernel);
                }
            return Instance;
        }

        /// <summary>
        ///     @TODO make a single global cancellation token that is passed to all objects
        ///     @TODO hook into dotnet process manager when main process recieves shutdown hit this method to cancel the global
        ///     token and have a clean system wide dispose, this will allow us to gracefully say bye to all peers and keep data
        ///     integrity.
        /// </summary>
        /// <returns></returns>
        public Task Shutdown()
        {
            var taskSource = new TaskCompletionSource<bool>();
            return taskSource.Task;
        }

        public void Dispose()
        {
            kernel?.Dispose();
        }
    }
}