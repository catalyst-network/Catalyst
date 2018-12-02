using System;
using Autofac;
using ADL.Peer;
using ADL.Rpc;
using ADL.Dfs;
using System.IO;
using ADL.Gossip;
using Akka.Actor;
using ADL.Consensus;
using ADL.Contract;
using ADL.Ledger;
using ADL.Node;
using System.Threading.Tasks;
using ADL.Mempool;
using Autofac.Core;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable
    {
        private Kernel Kernel { get; set; }
        private IRpcService RcpService { get; set; }
        private IDfsService DfsService { get; set; }
        private IPeerService PeerService { get; set; }
        private static AtlasSystem Instance { get; set; }
        private IGossipService GossipService { get; set; }
        private ILedgerService LedgerService { get; set; }
        private IMempoolService MempoolService { get; set; }
        private static readonly object Mutex = new object();
        private IContractService ContractService { get; set; }
        private IConsensusService ConsensusService { get; set; }
        
        /// <summary>
        /// Get a thread safe AtlasSystem singleton.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static AtlasSystem GetInstance(NodeOptions options)
        {
            if (Instance == null) 
            { 
                lock (Mutex)
                {
                    if (Instance == null) 
                    { 
                        Instance = new AtlasSystem(options);
                    }
                } 
            }
            return Instance;
        }
        
        /// <summary>
        /// Instantiates basic AtlasSystem.
        /// </summary>
        private AtlasSystem(NodeOptions options)
        {
            Kernel = ADL.Node.Kernel.GetInstance(options);

            if (options.Rpc)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    RcpService = scope.Resolve<IRpcService>();
                }
                RcpService.StartService();  
            }
            
            if (options.Consensus)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    ConsensusService = scope.Resolve<IConsensusService>();
                }
                RcpService.StartService();  
            }
            
            if (options.Contract)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    ContractService = scope.Resolve<IContractService>();
                }
                RcpService.StartService();  
            }
            
            if (options.Dfs)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    DfsService = scope.Resolve<IDfsService>();
                }
                DfsService.StartService(Kernel.Settings.Dfs);                   
            }
            
            if (options.Gossip)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    GossipService = scope.Resolve<IGossipService>();
                }
                RcpService.StartService();  
            }

            if (options.Ledger)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    LedgerService = scope.Resolve<ILedgerService>();
                }
                RcpService.StartService();     
            }
            
            if (options.Mempool)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    MempoolService = scope.Resolve<IMempoolService>();
                }
                RcpService.StartService();       
            }
            
            if (options.Peer)
            {
                Console.WriteLine("start p2p controller....");
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    PeerService = scope.Resolve<IPeerService>();
                }
                PeerService.StartServer(Kernel.Settings.Peer, new DirectoryInfo(Kernel.Settings.NodeOptions.DataDir));
            }         
        }

        public Task Shutdown()
        {
            var taskSource = new TaskCompletionSource<bool>();
            return taskSource.Task;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        public void Dispose()
        {
            RcpService?.StopService();
        }
    }
}
