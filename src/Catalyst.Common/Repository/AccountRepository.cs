using Catalyst.Common.Interfaces.Modules.Ledger;
using Catalyst.Common.Interfaces.Repository;
using SharpRepository.Repository;

namespace Catalyst.Common.Repository
{
    public class AccountRepository : RepositoryWrapper<IAccount, string>, IAccountRepository
    {
        public AccountRepository(IRepository<IAccount, string> repository) : base(repository)
        {
        }
    }
}
