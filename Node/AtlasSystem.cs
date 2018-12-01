using System;
using Autofac;
using ADL.Peer;
using ADL.Rpc;
using ADL.DFS;
using System.IO;
using ADL.Gossip;
using Akka.Actor;
using ADL.Consensus;
using ADL.Ledger;
using ADL.Node;
using System.Threading.Tasks;
using Autofac.Core;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
    {
        private IAdl Ledger { get; set; }
        public IKernel Kernel { get; set; }
        private IDFS DfsService { get; set; }
        private IPeer PeerService { get; set; }
        private IRpcService RcpService { get; set; }
        public IActorRef ContractSystem { get; set; }
        private ActorSystem ActorSystem { get; set; }
        private static AtlasSystem Instance { get; set; }
        private static readonly object Mutex = new object();

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
            using (ActorSystem = ActorSystem.Create("AtlasSystem"))
            {
                Kernel = StartUpRoutine.Boot(ActorSystem, options);

                if (options.Peer)
                {
                    StartPeer();        
                }

                if (options.Rpc)
                {
                    StartRpc();       
                }

                if (options.Dfs)
                {
                    StartDfs();                    
                }

                if (options.Gossip)
                {
                    StartGossip();
                }

                if (options.Contract)
                {
                    //@TODO
                }

                if (options.Consensus)
                {
                    StartConsensus();
                }
            }            
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartRpc()
        {
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                RcpService = scope.Resolve<IRpcService>();
            }
            RcpService.StartService();
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopRpc()
        {
            RcpService.StopService();
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartDfs()
        {
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                DfsService = scope.Resolve<IDFS>();
            }
            DfsService.Start(Kernel.Settings.Dfs);
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartConsensus()
        {
//            AdLedger.ConsensusService = ActorSystem.ActorOf(Kernel.Resolver.Create<ConsensusService>(), "ConsensusService");
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartGossip()
        {
//            AdLedger.GossipService = ActorSystem.ActorOf(Kernel.Resolver.Create<GossipService>(), "GossipService");
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartPeer()
        {
            Console.WriteLine("start p2p controller....");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                PeerService = scope.Resolve<IPeer>();
            }
            PeerService.StartServer(Kernel.Settings.Peer, new DirectoryInfo(Kernel.Settings.NodeOptions.DataDir));
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
