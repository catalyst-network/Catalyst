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
        private readonly IRepository<DeltaIndexDao> _repository;

        public DeltaIndexService(IRepository<DeltaIndexDao> repository) { _repository = repository; }

        public void Add(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            var g = _repository.GetAll();
            _repository.Add(deltaIndexes);
            var a = 0;
        }

        public void Add(DeltaIndexDao deltaIndex)
        {
            var g = _repository.GetAll();
            _repository.Add(deltaIndex);
            var a = 0;
        }

        public int Height()
        {
            var count = _repository.Count();
            if (_repository.Count() == 0)
            {
                return 0;
            }
            var max = _repository.Max(x => x.Height);
            return max;
        }
    }
}
