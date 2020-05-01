using System;
using System.Threading;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public sealed class PortMapper
    {
        public TimeSpan Timeout { get; } = TimeSpan.FromMilliseconds(100000);  
       
        public bool TryAddPortMapping(int port)
        {

            NatUtility.DeviceFound += (sender, args) => DeviceFound(sender, args, port);
            
            NatUtility.StartDiscovery();
            
            Thread.Sleep(Timeout);

            NatUtility.StopDiscovery();
            return true;
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
                    var mapping = await device.GetSpecificMappingAsync(Protocol.Tcp, port);
                } 
                catch 
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }

                // Try deleting the port we opened before:
                try
                {
                    await device.DeletePortMapAsync(mapping);
                    Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
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
                    var mapping = await device.GetSpecificMappingAsync(Protocol.Tcp, port);
                } 
                catch 
                {
                    Console.WriteLine("Couldn't get specific mapping");
                }

                // Try deleting the port we opened before:
                try
                {
                    await device.DeletePortMapAsync(mapping);
                    Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
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
