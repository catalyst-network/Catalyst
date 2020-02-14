using Catalyst.Abstractions.Sync.Interfaces;

namespace Catalyst.Core.Modules.Sync.Modal
{
    public class RankedItem<T> : IRankedItem<T>
    {
        public T Item { set; get; }
        public int Score { set; get; }
    }
}
