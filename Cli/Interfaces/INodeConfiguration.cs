namespace ADL.Cli.Interfaces
{
    public interface INodeConfiguration
    {
        IProtocolSettings Protocol { get; }
        IPathSettings Paths { get; }
        IP2PSettings P2P { get; }
        IRPCSettings RPC { get; }
    }
}
