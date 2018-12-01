namespace ADL.DFS
{
    public interface IDfsSettings
    {
        string StorageType { get; set; }
        ushort ConnectRetries { get; set; }
        string IpfsVersionApi { get; set; }
    }
}
