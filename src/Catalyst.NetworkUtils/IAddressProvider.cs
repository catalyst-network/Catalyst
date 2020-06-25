using System.Net;
using System.Threading.Tasks;

namespace Catalyst.NetworkUtils
{
    public interface IAddressProvider
    {
        Task<IPAddress> GetPublicIpAsync();
        Task<IPAddress> GetLocalIpAsync();
    }
}
