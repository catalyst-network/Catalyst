using System;
using System.Threading.Tasks;
using Akka.Actor;
using Autofac;
using Catalyst.Helpers.Util;
using Catalyst.Node.Modules.Core.Consensus;
using Catalyst.Node.Modules.Core.Contract;
using Catalyst.Node.Modules.Core.Dfs;
using Catalyst.Node.Modules.Core.Gossip;
using Catalyst.Node.Modules.Core.Ledger;
using Catalyst.Node.Modules.Core.Mempool;
using Catalyst.Node.Modules.Core.P2P;
using Catalyst.Node.Modules.Core.Rpc;

namespace Catalyst.Node
{
    public class CatalystNode : IDisposable
    {
        private static readonly object Mutex = new object();
        private readonly NodeOptions _options;

        /// <summary>
        ///     Instantiates basic CatalystSystem.
        /// </summary>
        private CatalystNode(NodeOptions options) // @TODO make kernel be a param
        {
            Guard.NotNull(options, nameof(options));
            _options = options;

            using (CatalystActorSystem = ActorSystem.Create("CatalystActorSystem"))
            {
            }

            Kernel = Kernel.GetInstance(_options);

            if (_options.Consensus)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    ConsensusService = scope.Resolve<IConsensusService>();
                }

                ConsensusService.StartService();
            }

            if (_options.Contract)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    ContractModule = scope.Resolve<IContractModule>();
                }

                ContractModule.StartService();
            }

            if (_options.Dfs)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    DfsModule = scope.Resolve<IDfsModule>();
                }

                DfsModule.StartService();
            }

            if (_options.Gossip)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    GossipModule = scope.Resolve<IGossipModule>();
                }

                GossipModule.StartService();
            }

            if (_options.Ledger)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    LedgerService = scope.Resolve<ILedgerService>();
                }

                LedgerService.StartService();
            }

            if (_options.Mempool)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    MempoolModule = scope.Resolve<IMempoolModule>();
                }

                MempoolModule.StartService();
            }

            if (_options.Peer)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    PeerService = scope.Resolve<IP2PModule>();
                }

                PeerService.StartService();
            }

            // @TODO we need to try catch all of this and gracefully exit if we cant start services required.
            if (_options.Rpc)
            {
                using (var scope = Kernel.Container.BeginLifetimeScope())
                {
                    RcpModule = scope.Resolve<IRpcModule>();
                }

                RcpModule.StartService();
//                RpcTaskManager = CatalystActorSystem.ActorOf<TaskManager>();
            }
        }

        private Kernel Kernel { get; }
        private IDfsModule DfsModule { get; }
        private IRpcModule RcpModule { get; }
        private IP2PModule PeerService { get; }
        private static CatalystNode Instance { get; set; }
        private IGossipModule GossipModule { get; }
        private ILedgerService LedgerService { get; }
        private IContractModule ContractModule { get; }
        private IConsensusService ConsensusService { get; }
        private static ActorSystem CatalystActorSystem { get; set; }

        /// <summary>
        ///     Get reference to actor (static)
        /// </summary>
        /// <returns>IActorRef</returns>
        public static IActorRef RpcTaskManager { get; private set; }

        /// <summary>
        ///     Get mempool implementation (static)
        /// </summary>
        /// <returns>IMempoolService</returns>
        public static IMempoolModule MempoolModule { get; private set; }

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

        /// <summary>
        ///     Get a thread safe CatalystSystem singleton.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CatalystNode GetInstance(NodeOptions options)
        {
            Guard.NotNull(options, nameof(options));
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new CatalystNode(options);
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
    }
}