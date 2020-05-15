using System;
using Catalyst.Modules.UPnP;
using Mono.Nat;

namespace Catalyst.UPnP.Tests.TestUtils
{
    public class TestNatUtilityProvider : INatUtilityProvider
    {
        private readonly INatDevice _device;
        public TestNatUtilityProvider(INatDevice device)
        {
            _device = device;
        }
        public event EventHandler<DeviceEventArgs> DeviceFound;
        public void StartDiscovery()
        {
            DeviceFound?.Invoke(this, new DeviceEventArgs(_device));
        }

        public void StopDiscovery()
        {
        }
    }

}
