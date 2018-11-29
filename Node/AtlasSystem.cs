using System;
using Autofac;
using ADL.P2P;
using ADL.Rpc;
using ADL.DFS;
using System.IO;
using ADL.Gossip;
using Akka.Actor;
using ADL.Consensus;
using ADL.Node.Ledger;
using ADL.Node.Interfaces;
using System.Threading.Tasks;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
    {
        private IADL AdLedger { get; set; }
        public IKernel Kernel { get; set; }
        private IDFS DfsService { get; set; }
        private IP2P P2PService { get; set; }
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

                if (options.P2P)
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
            RcpService.StartServer(Kernel.Settings.NodeConfiguration.Rpc);
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopRpc()
        {
            RcpService.StopServer();
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
            DfsService.Start(Kernel.Settings.NodeConfiguration.Dfs);
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartConsensus()
        {
            AdLedger.ConsensusService = ActorSystem.ActorOf(Kernel.Resolver.Create<ConsensusService>(), "ConsensusService");
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartGossip()
        {
            AdLedger.GossipService = ActorSystem.ActorOf(Kernel.Resolver.Create<GossipService>(), "GossipService");
        }

        /// <summary>
        /// 
        /// </summary>
        public void StartPeer()
        {
            Console.WriteLine("start p2p controller....");
            using (var scope = Kernel.Container.BeginLifetimeScope())
            {
                P2PService = scope.Resolve<IP2P>();
            }
            P2PService.StartServer(Kernel.Settings.NodeConfiguration.P2P, Kernel.Settings.NodeConfiguration.Ssl, new DirectoryInfo(Kernel.Settings.NodeConfiguration.NodeOptions.DataDir));
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
            RcpService?.StopServer();
        }
    }
}
