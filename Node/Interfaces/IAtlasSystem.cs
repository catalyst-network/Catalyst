namespace ADL.Node.Interfaces
{
    public interface IAtlasSystem
    {        
        IKernel Kernel { get; set; }
        void StartConsensus();
        void StartGossip();
        void StartRpc();
        void StartDfs();
        void StartPeer();
        void Dispose();
    }
}