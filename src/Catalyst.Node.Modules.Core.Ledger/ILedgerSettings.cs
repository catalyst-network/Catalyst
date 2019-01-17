namespace Catalyst.Node.Modules.Core.Ledger
{
    public interface ILedgerSettings
    {
        string Chain { get; set; }
        string Index { get; set; }
    }
}