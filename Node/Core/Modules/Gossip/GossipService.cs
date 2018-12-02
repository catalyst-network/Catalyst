using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Gossip
{   
    public class GossipService : ServiceBase, IGossipService
    {
        private IGossip Gossip;
        private IGossipSettings GossipSettings;
        
        /// <summary>
        /// 
        /// </summary>
        public GossipService(IGossip gossip, IGossipSettings gossipSettings)
        {
            Gossip = gossip;
            GossipSettings = gossipSettings;
        }
    }
}
