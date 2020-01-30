using Catalyst.Protocol.Deltas;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Modules.Sync.Modal
{
    public class DeltaIndexScore
    {
        public int Score { set; get; }
        public RepeatedField<DeltaIndex> DeltaIndexes { set; get; }
    }
}
