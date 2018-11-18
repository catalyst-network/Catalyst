namespace ADL.Cli.Shell
{
    internal interface IShell
    {    
        void RunConsole();
        bool OnCommand(string[] args);
        bool OnGetCommand(string[] args);
        bool OnRpcCommand(string[] args);
        bool OnDfsCommand(string[] args);
        bool OnWalletCommand(string[] args);
        bool OnPeerCommand(string[] args);
        bool OnGossipCommand(string[] args);
        bool OnConsensusCommand(string[] args);
        bool OnServiceCommand(string[] args);
    }
}