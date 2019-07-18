using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Rpc.Authentication;
using SharpRepository.Repository;

namespace Catalyst.Common.Repository
{
    public class AuthCredentialRepository : RepositoryWrapper<AuthCredentials>, IAuthCredentialRepository
    {
        public AuthCredentialRepository(IRepository<AuthCredentials, string> repository) : base(repository) { }
    }
}
