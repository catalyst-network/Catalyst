using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Google.Protobuf.Collections;
using System.Linq;
using System.Threading;

namespace Catalyst.Core.Modules.Sync
{
    public class DeltaHistoryRanker : PeerMessageRankManager<RepeatedField<DeltaIndex>, int>
    {
        private int _totalScore;
        public int TotalScore => _totalScore;

        public int Height { set; get; }
        public DeltaHistoryRequest DeltaHistoryRequest { private set; get; }

        public DeltaHistoryRanker(DeltaHistoryRequest deltaHistoryRequest)
        {
            DeltaHistoryRequest = deltaHistoryRequest;
        }

        public void Add(RepeatedField<DeltaIndex> key)
        {
            Interlocked.Increment(ref _totalScore);

            if (_messages.ContainsKey(key))
            {
                _messages[key]++;
                return;
            }

            _messages.Add(key, 1);
        }

        public int GetHighestScore()
        {
            if (_messages.Count == 0)
            {
                return 0;
            }
            return _messages.Values.Max(x => x);
        }

        public RepeatedField<DeltaIndex> GetMostPopular() => _messages.Where(x => x.Value >= GetHighestScore()).Select(x=>x.Key)?.FirstOrDefault();
    }
}
