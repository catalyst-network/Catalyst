namespace ADL.Ledger
{
    public interface ILedgerSettings
    {
        string Chain { get; set; }
        string Index { get; set; }
    }
}