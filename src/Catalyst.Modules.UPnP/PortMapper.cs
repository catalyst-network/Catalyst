using System;
using System.Threading;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    
    public sealed class PortMapper
    {
        private INatUtilityProvider _natUtilityProvider;
        public PortMapper(INatUtilityProvider natUtilityProvider)
        {
            _natUtilityProvider = natUtilityProvider;
        }
        public event EventHandler TimeoutReached;
        
       
        public void TryGetDevice(int port, int timeoutInSeconds = 30)
        {
            var started = DateTime.Now;
            var timespan = TimeSpan.FromSeconds(timeoutInSeconds);  
            //NatUtility.DeviceFound += (sender, args) => TryAddPort(sender, args, port);
            _natUtilityProvider.DeviceFound += TryGetPorts;
            
            _natUtilityProvider.StartDiscovery();
            Console.WriteLine("started searching");
            while (DateTime.Now <= started + timespan)
            {
                //Thread.Sleep(5000);
            }
           

            _natUtilityProvider.StopDiscovery();
            Console.WriteLine("stopped searching");

            OnTimeoutReached();
        }

        private void OnTimeoutReached()
        {
            EventHandler handler = TimeoutReached;
            handler?.Invoke(this, EventArgs.Empty);
        }
        
        public void TryGetPortMappings(int timeoutInSeconds = 30)
        {
            var timespan = TimeSpan.FromMilliseconds(timeoutInSeconds);  
            NatUtility.DeviceFound += TryGetPorts;
            
            NatUtility.StartDiscovery();
            
            Thread.Sleep(timespan);

            NatUtility.StopDiscovery();
        }
        

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
     
        private async void TryAddPort(object sender, DeviceEventArgs args, int port)
        { 
            await _locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;

                // Try to create a new port map:
                var mapping = new Mapping(Protocol.Tcp, port, port);
                await device.CreatePortMapAsync(mapping);
                
                // Try to retrieve confirmation on the port map we just created:
                try 
                {
                    var map = await device.GetSpecificMappingAsync(Protocol.Tcp, port);
                } 
                catch 
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }
            } 
            finally 
            {
                _locker.Release();
            }
        }
        
        private async void TryGetPorts(object sender, DeviceEventArgs args)
        { 
            await _locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;

                try 
                {
                    var mappings = await device.GetAllMappingsAsync();
                    if (mappings.Length == 0)
                        Console.WriteLine("No existing uPnP mappings found.");
                    foreach (Mapping mp in mappings)
                        Console.WriteLine("Existing Mapping: protocol={0}, public={1}, private={2}", mp.Protocol, mp.PublicPort, mp.PrivatePort);

                } 
                catch 
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }
            } 
            finally 
            {
                _locker.Release();
            }
        }
        
        private async void TryDeletePort(object sender, DeviceEventArgs args, int port)
        { 
            await _locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;

                // Try to create a new port map:
                var mapping = new Mapping(Protocol.Tcp, port, port);
                await device.CreatePortMapAsync(mapping);
                
                // Try to retrieve confirmation on the port map we just created:
                try 
                {
                    var map = await device.GetSpecificMappingAsync(Protocol.Tcp, port);
                } 
                catch 
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }

                // Try deleting the port we opened before:
                try
                {
                    var map = await device.DeletePortMapAsync(mapping);
                    Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", map.Protocol, map.PublicPort, map.PrivatePort);
                } 
                catch 
                {
                    Console.WriteLine("Couldn't delete specific mapping");
                }
            } 
            finally 
            {
                _locker.Release();
            }
        }
    }
    
}
