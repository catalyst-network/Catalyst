namespace ADL.Node.Core.Modules.Network
{
    public interface INetworkSettings
    {
        uint Magic { get; set; }
        int Port { get; set; }
        string BindAddress { get; }
        string[] SeedList { get; set; }
        byte AddressVersion { get; set; }
    }
}
