using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Catalyst.Abstractions.Sync.Interfaces
{
    public interface IDeltaHeightRanker : IDisposable
    {
         IEnumerable<PeerId> GetPeers();

         void Add(PeerId key, LatestDeltaHashResponse value);

         int Count();

         IOrderedEnumerable<IRankedItem<LatestDeltaHashResponse>> GetMessagesByMostPopular(Func<KeyValuePair<PeerId, LatestDeltaHashResponse>, bool> filter = null);
    }
}
