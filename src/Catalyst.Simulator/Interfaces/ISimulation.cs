using System.Collections.Generic;
using System.Threading.Tasks;

namespace Catalyst.Simulator.Interfaces
{
    public interface ISimulation
    {
        Task SimulateAsync(IList<ClientRpcInfo> clientRpcInfoList);
    }
}
