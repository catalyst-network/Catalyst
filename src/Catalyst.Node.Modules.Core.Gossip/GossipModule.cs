using System.Threading.Tasks;
using Akka.Actor;
using Autofac;

namespace Catalyst.Node.Modules.Core.Gossip
{

    public class GossipModule : AsyncModuleBase, IGossipModule
    {
        private IActorRef Gossip;
        private IGossipSettings GossipSettings;
        
        public static ContainerBuilder Load(ContainerBuilder builder, IGossipSettings gossipSettings)
        {
            builder.Register(c => new GossipModule(gossipSettings))
                .As<IGossipModule>()
                .InstancePerLifetimeScope();
            return builder;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public GossipModule(IGossipSettings gossipSettings)
        {
            //@TODO guard util
            GossipSettings = gossipSettings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
