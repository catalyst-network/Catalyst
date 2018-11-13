namespace ADL.Node.Interfaces
{
    public interface IAtlasSystem
    {
        void StartConsensus();

        void StartNode();

        void StartRcp();

        void Dispose();
    }
}