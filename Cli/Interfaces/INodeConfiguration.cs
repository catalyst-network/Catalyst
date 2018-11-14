namespace ADL.Cli.Interfaces
{
    public interface INodeConfiguration
    {
        uint Magic { get; }
        byte AddressVersion { get; }
        string[] SeedList { get; }
        IPathSettings Paths { get; }
        IP2PSettings P2P { get; }
        IRPCSettings RPC { get; }
    }
}
