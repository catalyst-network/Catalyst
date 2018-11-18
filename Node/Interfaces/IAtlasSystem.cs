namespace ADL.Node.Interfaces
{
    public interface IAtlasSystem
    {        
        void StartConsensus();
        void StartGossip();
        void StartRpc();
        void StartDfs();
        void Dispose();
    }
}