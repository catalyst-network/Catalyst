namespace ADL.Peer
{
    public interface IPeerSettings
    {
        ushort Port { get; set; }
        uint Magic { get; set; }
        string[] SeedList { get; set; }
        byte AddressVersion { get; set; }
    }
}