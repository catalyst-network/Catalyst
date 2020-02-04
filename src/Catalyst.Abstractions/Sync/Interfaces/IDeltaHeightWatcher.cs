using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Protocol.Deltas;

namespace Catalyst.Abstractions.Sync.Interfaces
{
    public interface IDeltaHeightWatcher : IDisposable
    {
        DeltaIndex LatestDeltaHash { get; }
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        Task WaitForDeltaHeightAsync(int currentDeltaIndex, CancellationToken cancellationToken);
    }
}
