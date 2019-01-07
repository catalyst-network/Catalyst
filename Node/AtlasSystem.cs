using System;
using System.IO;
using Autofac;
using System.Threading.Tasks;
using ADL.Node.Core.Modules.Dfs;
using ADL.Node.Core.Modules.Rpc;
using ADL.Node.Core.Modules.Network;
using ADL.Node.Core.Modules.Gossip;
using ADL.Node.Core.Modules.Ledger;
using ADL.Node.Core.Modules.Mempool;
using ADL.Node.Core.Modules.Contract;
using ADL.Node.Core.Modules.Consensus;
using Akka.Actor;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable
    {
        private Kernel Kernel { get; set; }
        private IRpcService RcpService { get; set; }
        private IDfsService DfsService { get; set; }
        private INetworkService PeerService { get; set; }
        private static AtlasSystem Instance { get; set; }
        private IGossipService GossipService { get; set; }
        private ILedgerService LedgerService { get; set; }        
        private static readonly object Mutex = new object();
        private IContractService ContractService { get; set; }
        private IConsensusService ConsensusService { get; set; }
        private static ActorSystem MainActorSystem { get; set; }
        
        /// <summary>
        /// Get reference to actor (static)
        /// </summary>
        /// <returns>IActorRef</returns>
        public static IActorRef TaskHandlerActor { get; private set; }
        
        /// <summary>
        /// Get mempool implementation (static)
        /// </summary>
        /// <returns>IMempoolService</returns>
        public static IMempoolService MempoolService { get; private set; }

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
            MainActorSystem = ActorSystem.Create("AtlasActorSystem");
            TaskHandlerActor = MainActorSystem.ActorOf<TaskHandlerActor>();
            
            Kernel = Kernel.GetInstance(options);

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
                ConsensusService.StartService();  
            }
            
            if (options.Contract)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    ContractService = scope.Resolve<IContractService>();
                }
                ContractService.StartService();  
            }
            
            if (options.Dfs)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    DfsService = scope.Resolve<IDfsService>();
                }
                DfsService.StartService();
            }
            
            if (options.Gossip)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    GossipService = scope.Resolve<IGossipService>();
                }
                GossipService.StartService();  
            }

            if (options.Ledger)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    LedgerService = scope.Resolve<ILedgerService>();
                }
                LedgerService.StartService();     
            }
            
            if (options.Mempool)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    MempoolService = scope.Resolve<IMempoolService>();
                }
                MempoolService.StartService();       
            }
            
            if (options.Peer)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    PeerService = scope.Resolve<INetworkService>();
                }
                PeerService.StartService();
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
            PeerService?.StopService();
            RcpService?.StopService();
            MainActorSystem.Stop(TaskHandlerActor);
            MainActorSystem.Dispose();
        }
    }
}
