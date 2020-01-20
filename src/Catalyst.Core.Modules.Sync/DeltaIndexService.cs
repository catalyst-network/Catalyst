using Catalyst.Core.Lib.DAO.Ledger;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Sync
{
    public interface IDeltaIndexService
    {
        void Add(DeltaIndexDao deltaIndex);
        int Height();
    }

    public class DeltaIndexService : IDeltaIndexService
    {
        private readonly IRepository<DeltaIndexDao> _repository;

        public DeltaIndexService(IRepository<DeltaIndexDao> repository) { _repository = repository; }

        public void Add(DeltaIndexDao deltaIndex) { _repository.Add(deltaIndex); }

        public int Height() { return _repository.Max(x => x.Height); }
    }
}
