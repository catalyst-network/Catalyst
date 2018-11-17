namespace ADL.Node.Interfaces
{
    public interface IAtlasSystem
    {        
        void StartConsensus();

        void StartGossip();

        void StartRcp();

        void StartDfs();

        void Dispose();
    }
}