using System.Collections.Generic;
using System.Linq;
using Catalyst.Core.Lib.DAO.Ledger;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.Service
{
    public class DeltaIndexService : IDeltaIndexService
    {
        private readonly IRepository<DeltaIndexDao, string> _repository;

        public DeltaIndexService(IRepository<DeltaIndexDao, string> repository) { _repository = repository; }

        public void Add(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            var c = deltaIndexes.Count();
            _repository.Add(deltaIndexes);
            if (_repository.Count() > 100)
            {
                var a = 0;
            }
        }

        public void Add(DeltaIndexDao deltaIndex)
        {
            _repository.Add(deltaIndex);
            if (_repository.Count() > 100)
            {
                var a = 0;
            }
        }

        public IEnumerable<DeltaIndexDao> GetRange(int start, int count)
        {
            return _repository.FindAll(x => x.Height >= start && x.Height < start + count).OrderBy(x => x.Height);
        }

        public int Height()
        {
            return _repository.Count() == 0 ? 0 : _repository.Max(x => x.Height);
        }

        public DeltaIndexDao LatestDeltaIndex() { return _repository.Find(x => x.Height == Height()); }
    }
}
