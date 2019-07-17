using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.Repository;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Repository
{
    public class MempoolRepository : RepositoryWrapper<IMempoolDocument, string>, IMempoolRepository
    {
        public MempoolRepository(IRepository<IMempoolDocument, string> repository) : base(repository)
        {
        }
    }
}
