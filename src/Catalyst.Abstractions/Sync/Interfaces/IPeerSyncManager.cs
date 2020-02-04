using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Deltas;
using Google.Protobuf;
using Google.Protobuf.Collections;

namespace Catalyst.Abstractions.Sync.Interfaces
{
    public interface IPeerSyncManager : IDisposable
    {
        int PeerCount { get; }
        int MaxSyncPoolSize { get; }
        IObservable<RepeatedField<DeltaIndex>> ScoredDeltaIndexRange { get; }
        bool IsPoolAvailable();
        bool PeersAvailable();
        bool ContainsPeerHistory();
        void GetDeltaIndexRangeFromPeers(int index, int range);
        void GetDeltaHeight();
        Task WaitForPeersAsync(CancellationToken cancellationToken = default);
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
