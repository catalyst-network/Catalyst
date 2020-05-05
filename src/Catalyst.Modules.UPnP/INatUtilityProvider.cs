using System;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public interface INatUtilityProvider
    {
        event EventHandler<DeviceEventArgs> DeviceFound;

        void StartDiscovery();
        
        void StopDiscovery();
    }
}
