namespace ADL.Dfs
{
    public interface IDfsSettings
    {
        string StorageType { get; set; }
        ushort ConnectRetries { get; set; }
        string IpfsVersionApi { get; set; }
    }
}
