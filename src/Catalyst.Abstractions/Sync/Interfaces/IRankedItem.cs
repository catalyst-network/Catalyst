namespace Catalyst.Abstractions.Sync.Interfaces
{
    public interface IRankedItem<T>
    {
        T Item { set; get; }
        int Score { set; get; }
    }
}
