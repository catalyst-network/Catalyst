namespace Catalyst.Core.Modules.Sync
{
    public interface ISync
    {
        void SetDeltaCurrentIndex(int currentDeltaIndex);
        void Start();
    }
}
