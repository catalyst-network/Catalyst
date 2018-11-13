namespace ADL.Cli.Interfaces
{
    public interface INodeConfiguration
    {
        uint Magic { get; }
        byte AddressVersion { get; }
        string[] SeedList { get; }
        string ChainPath { get; }
        string ChainIndex { get; }
        int P2PPort { get; }
        int RPCAddress { get; }
        int RPCPort { get; }
    }
}