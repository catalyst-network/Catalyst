using System;
using System.Threading.Tasks;
using Akka.Actor;
using Autofac;

namespace Catalyst.Node.Modules.Core.Gossip
{
    public class GossipModule : Module
    {
        private IActorRef Gossip;
        private IGossipSettings GossipSettings;

        /// <summary>
        /// </summary>
        public GossipModule(IGossipSettings gossipSettings)
        {
            //@TODO guard util
            GossipSettings = gossipSettings;
        }
        
        /// <summary>
        ///     Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IGossip GetImpl()
        {
            throw new NotImplementedException();
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