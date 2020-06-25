using System.Net;
using System.Threading.Tasks;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public interface IUPnPUtility
    {
        public Task<UPnPConstants.Result> MapPorts(Mapping[] ports,
            int timeoutInSeconds = UPnPConstants.DefaultTimeout, bool delete = false);

        public Task<IPAddress> GetPublicIpAddress(int timeoutInSeconds = UPnPConstants.DefaultTimeout);
    }
}
