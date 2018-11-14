using System;
using ADL.Node.Interfaces;

namespace ADL.Node
{
    public class AtlasSystem : IDisposable, IAtlasSystem
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
        
        public void StartNode()
        {
            Console.WriteLine("Node starting....");
        }
        
        public void StartRcp()
        {
            Console.WriteLine("RCP server starting....");
        }
    }
}