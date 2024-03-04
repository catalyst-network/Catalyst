using Nethermind.Db;

namespace Catalyst.Core.Modules.Kvm
{
    public interface ISnapshotableDb : IDb
    {
        public const int NoChangesCheckpoint = -1;

        void Restore(int snapshot);

        void Commit();

        int TakeSnapshot();

        bool HasUncommittedChanges => TakeSnapshot() != NoChangesCheckpoint;
    }
}
