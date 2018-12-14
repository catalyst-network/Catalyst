namespace ADL.Node.Core.Modules.Peer
{
    public interface IPeerSettings
    {
        string BindAddress { get; set; }
        ushort Port { get; set; }
        uint Magic { get; set; }
        string[] SeedList { get; set; }
        byte AddressVersion { get; set; }
    }
}