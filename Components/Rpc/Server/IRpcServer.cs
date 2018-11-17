namespace ADL.Rpc.Server
{
    public interface IRpcServer
    {
        void StartServer(IRpcSettings settings);
        void StopServer();
    }
}