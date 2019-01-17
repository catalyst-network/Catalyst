using System;
using Autofac;
using Akka.Actor;
using Catalyst.Helpers.Util;
using System.Threading.Tasks;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Rpc;
using Catalyst.Node.Modules.Core.P2P;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.Mempool;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Consensus;

namespace Catalyst.Node
{
    public class CatalystNode : IDisposable
    {
        private Kernel Kernel { get; set; }
        private IDfsModule DfsModule { get; set; }
        private IRpcModule RcpModule { get; set; }
        private IP2PModule PeerService { get; set; }
        private static CatalystNode Instance { get; set; }
        private IGossipModule GossipModule { get; set; }
        private ILedgerService LedgerService { get; set; }        
        private IContractModule ContractModule { get; set; }
        private IConsensusService ConsensusService { get; set; }
        private static ActorSystem CatalystActorSystem { get; set; }

        private readonly NodeOptions Options;
        private static readonly object Mutex = new object();

        /// <summary>
        /// Get reference to actor (static)
        /// </summary>
        /// <returns>IActorRef</returns>
        public static IActorRef RpcTaskManager { get; private set; }
        
        /// <summary>
        /// Get mempool implementation (static)
        /// </summary>
        /// <returns>IMempoolService</returns>
        public static IMempoolModule MempoolModule { get; private set; }

        /// <summary>
        /// Get a thread safe CatalystSystem singleton.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CatalystNode GetInstance(NodeOptions options)
        {
            Guard.NotNull(options, nameof(options));
            if (Instance == null) 
            { 
                lock (Mutex)
                {
                    if (Instance == null) 
                    { 
                        //@TODO try instantiate kernel here and pass to CatalystNode as param
                        Instance = new CatalystNode(options);
                    }
                } 
            }
            return Instance;
        }
        
        /// <summary>
        /// Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(NodeOptions options) // @TODO make kernel be a param
        {
            Guard.NotNull(options, nameof(options));
            Options = options;

            using (CatalystActorSystem = ActorSystem.Create("CatalystActorSystem"))
            {
                
            }
            
            Kernel = Kernel.GetInstance(options);
            
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
                    ContractModule = scope.Resolve<IContractModule>();
                }
                ContractModule.StartService();  
            }
            
            if (options.Dfs)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    DfsModule = scope.Resolve<IDfsModule>();
                }
                DfsModule.StartService();
            }
            
            if (options.Gossip)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    GossipModule = scope.Resolve<IGossipModule>();
                }
                GossipModule.StartService();  
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
                    MempoolModule = scope.Resolve<IMempoolModule>();
                }
                MempoolModule.StartService();       
            }
            
            if (options.Peer)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    PeerService = scope.Resolve<IP2PModule>();
                }
                PeerService.StartService();
            }
            
            // @TODO we need to try catch all of this and gracefully exit if we cant start services required.
            if (options.Rpc)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    RcpModule = scope.Resolve<IRpcModule>();
                }

                RcpModule.StartService();
//                RpcTaskManager = CatalystActorSystem.ActorOf<TaskManager>();
            }
        }

        /// <summary>
        /// @TODO make a single global cancellation token that is passed to all objects
        /// @TODO hook into dotnet process manager when main process recieves shutdown hit this method to cancel the global token and have a clean system wide dispose, this will allow us to gracefully say bye to all peers and keep data integrity.
        /// </summary>
        /// <returns></returns>
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
            RcpModule?.StopService();
            CatalystActorSystem.Stop(RpcTaskManager);
            CatalystActorSystem.Dispose();
        }
    }
}
