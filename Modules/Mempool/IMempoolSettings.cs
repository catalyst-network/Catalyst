namespace ADL.Mempool
{
    public interface IMempoolSettings
    {
        string Type { get; set; }
        ushort SaveAfterSeconds { get; set; }
        ushort SaveAfterChanges { get; set; }
        string AllowAdmin { get; set; }
    }
}