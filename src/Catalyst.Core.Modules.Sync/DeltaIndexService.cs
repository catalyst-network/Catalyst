using System.Collections.Generic;
using Catalyst.Core.Lib.DAO.Ledger;
using SharpRepository.Repository;

namespace Catalyst.Core.Modules.Sync
{
    public interface IDeltaIndexService
    {
        void Add(DeltaIndexDao deltaIndex);
        void Add(IEnumerable<DeltaIndexDao> deltaIndexes);
        int Height();
    }

    public class DeltaIndexService : IDeltaIndexService
    {
        private readonly IRepository<DeltaIndexDao, string> _repository;

        public DeltaIndexService(IRepository<DeltaIndexDao, string> repository) { _repository = repository; }

        public void Add(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            _repository.Add(deltaIndexes);
        }

        public void Add(DeltaIndexDao deltaIndex)
        {
            _repository.Add(deltaIndex);
        }

        public int Height()
        {
            if (_repository.Count() == 0)
            {
                return 0;
            }

            return _repository.Max(x => x.Height);
        }
    }
}
