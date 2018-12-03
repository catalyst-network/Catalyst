namespace ADL.Node.Core.Modules.Dfs
{
    public interface IDfsSettings
    {
        string StorageType { get; set; }
        ushort ConnectRetries { get; set; }
        string IpfsVersionApi { get; set; }
    }
}
