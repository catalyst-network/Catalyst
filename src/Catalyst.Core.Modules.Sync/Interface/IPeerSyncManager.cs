using System;
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
        void GetDeltaIndexRangeFromPeers(IMessage message);
        void GetDeltaHeight();
        Task StartAsync();
    }
}
