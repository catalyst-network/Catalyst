using System.Collections.Generic;
using Catalyst.Core.Lib.DAO.Ledger;

namespace Catalyst.Core.Lib.Service
{
    public interface IDeltaIndexService
    {
        void Add(DeltaIndexDao deltaIndex);
        void Add(IEnumerable<DeltaIndexDao> deltaIndexes);
        int Height();
        IEnumerable<DeltaIndexDao> GetRange(int start, int count);
        DeltaIndexDao LatestDeltaIndex();
    }
}
