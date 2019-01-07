using System.Threading;
using System.Threading.Tasks;
using ADL.Node.Core.Helpers.Services;
using ADL.Transaction;
using Akka.Actor;

namespace ADL.Node.Core.Modules.Gossip
{

    public class GossipService : AsyncServiceBase, IGossipService
    {
        private IActorRef Gossip;
        private IGossipSettings GossipSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public GossipService(IGossipSettings gossipSettings)
        {
            GossipSettings = gossipSettings;
        }
        
        public override bool StartService()
        {
            Task.Run(RunAsyncActors).Wait();
            return true;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IGossip GetImpl()
        {
            throw new System.NotImplementedException();
        }

        private async Task RunAsyncActors()
        {
            using (var gossipSystem = ActorSystem.Create("GossipSystem"))
            {
                Gossip = gossipSystem.ActorOf(Props.Create(() => new GossipActor()), "GossipActor");
            }
        }
    }
}
