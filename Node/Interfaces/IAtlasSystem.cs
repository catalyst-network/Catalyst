namespace ADL.Node.Interfaces
{
    public interface IAtlasSystem
    {   
        void Dispose();
        void StartRpc();
        void StartDfs();
        void StartPeer();
        void StartGossip();
        void StartConsensus();
        IKernel Kernel { get; set; }        
    }
}
