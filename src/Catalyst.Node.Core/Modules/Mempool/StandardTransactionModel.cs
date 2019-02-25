using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Protocols.Transaction;

namespace Catalyst.Node.Core.Modules.Mempool
{
    public class StTxModel
    {
        [SharpRepository.Repository.RepositoryPrimaryKey]
        public Key Key { get; set; }
        public StTx Transaction { get; set; }
    }
}
