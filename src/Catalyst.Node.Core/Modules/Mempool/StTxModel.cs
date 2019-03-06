using Catalyst.Protocols.Transaction;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Modules.Mempool
{
    public class StTxModel
    {
        [RepositoryPrimaryKey]
        public Key Key { get; set; }

        public StTx Transaction { get; set; }
    }
}