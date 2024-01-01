#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

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
            Height = (int) deltaHistoryRequest.Height;
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
