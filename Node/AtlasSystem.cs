using System;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable
    {        
        public AtlasSystem()
        {
            
        }
        
        public void Dispose()
        {
//            RpcServer?.Dispose();
//            ActorSystem.Stop(LocalNode);
//            _actorSystem.Dispose();
        }
        
        public void StartConsensus()
        {
            Console.WriteLine("Consensus starting....");
        }
        
        public void StartNode(int port = 0, int ws_port = 0)
        {
            Console.WriteLine("Node starting....");
        }
        
        public void StartRcp()
        {
            Console.WriteLine("RCP server starting....");
        }
    }
}