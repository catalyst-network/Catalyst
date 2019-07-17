using Catalyst.Common.Interfaces.Repository;
using SharpRepository.Repository;

namespace Catalyst.Common.Repository
{
    public class RepositoryWrapper<T, TKey> : IRepositoryWrapper<T, TKey> where T : class
    {
        public IRepository<T, TKey> Repository { get; }

        public RepositoryWrapper(IRepository<T, TKey> repository)
        {
            Repository = repository;
        }

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
