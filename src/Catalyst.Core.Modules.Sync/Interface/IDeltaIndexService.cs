using System.Collections.Generic;
using Catalyst.Core.Lib.DAO.Ledger;

namespace Catalyst.Core.Modules.Sync.Interface
{
    public interface IDeltaIndexService
    {
        void Add(DeltaIndexDao deltaIndex);
        void Add(IEnumerable<DeltaIndexDao> deltaIndexes);
        int Height();
    }
}
