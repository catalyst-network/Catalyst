using System;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public class NatUtilityProvider : INatUtilityProvider
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        public void StartDiscovery()
        {
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.StartDiscovery();
        }

        public void StopDiscovery()
        {
            NatUtility.DeviceFound -= DeviceFound;
            NatUtility.StopDiscovery();
        }

    }
}
