namespace Catalyst.Node.Modules.Core.Mempool
{
    public interface IMempoolSettings
    {
        string Type { get; set; }
        string When { get; set; }
    }
}