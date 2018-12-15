namespace ADL.Node.Core.Modules.Mempool
{
    public interface IMempoolSettings
    {
        string Type { get; set; }
        ushort SaveAfterSeconds { get; set; }
        ushort SaveAfterChanges { get; set; }
        string AllowAdmin { get; set; }
        string When { get; set; }
    }
}