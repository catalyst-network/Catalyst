namespace ADL.Rpc
{
    public interface IRpcService
    {
        void StartServer(IRpcSettings settings);
        void StopServer();
    }
}