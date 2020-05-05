using System;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public class NatUtilityProvider : INatUtilityProvider
    {

        public event EventHandler<DeviceEventArgs> DeviceFound;

        public void StartDiscovery()
        {
            NatUtility.DeviceFound += (EventHandler<DeviceEventArgs>) ((o, e) =>
            {
                EventHandler<DeviceEventArgs> deviceFound = DeviceFound;
                if (deviceFound == null)
                    return;
                deviceFound(o, e);
            });
            NatUtility.StartDiscovery();
        }

        public void StopDiscovery()
        {
            NatUtility.DeviceFound -= (EventHandler<DeviceEventArgs>) ((o, e) =>
            {
                EventHandler<DeviceEventArgs> deviceFound = DeviceFound;
                if (deviceFound == null)
                    return;
                deviceFound(o, e);
            });
            NatUtility.StopDiscovery();
        }

    }
}
