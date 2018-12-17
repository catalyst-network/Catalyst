namespace ADL.Node.Core.Modules.Network
{
    public interface INetworkSettings
    {
        string BindAddress { get; }
        ushort Port { get; set; }
        uint Magic { get; set; }
        string[] SeedList { get; set; }
        byte AddressVersion { get; set; }
    }
}