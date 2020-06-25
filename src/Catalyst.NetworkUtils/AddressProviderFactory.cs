using Catalyst.Modules.UPnP;
using Serilog.Core;

namespace Catalyst.NetworkUtils
{
    public static class AddressProviderFactory
    {
        public static AddressProvider Create()
        {
            return new AddressProvider(
                new UPnPUtility(new NatUtilityProvider(), Logger.None),
                new WebClientWrapperFactory(),
                new SocketWrapperFactory()
                );
        }
    }
}
