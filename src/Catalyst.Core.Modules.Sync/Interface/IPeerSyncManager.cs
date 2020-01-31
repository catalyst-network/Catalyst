using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Core.Modules.Sync.Interface
{
    public interface IPeerSyncManager : IDisposable
    {
        int PeerCount { get; }
        IObservable<RepeatedField<DeltaIndex>> ScoredDeltaIndexRange { get; }
        void GetDeltaIndexRangeFromPeers(int index, int range);
        void GetDeltaHeight();
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
