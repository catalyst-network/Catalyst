using System;
using System.Threading;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    public sealed class PortMapper
    {
        public TimeSpan Timeout { get; } = TimeSpan.FromMilliseconds(100000);  
       
        public bool Run()
        {
            NatUtility.DeviceFound += DeviceFound;
            
            NatUtility.StartDiscovery();
            
            Thread.Sleep(Timeout);

            NatUtility.StopDiscovery();
            return true;
        }
        
        public 

        readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
     
        private async void DeviceFound(object sender, DeviceEventArgs args)
        { 
            await _locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;

                // Try to create a new port map:
                var mapping = new Mapping(Protocol.Tcp, 6001, 6001);
                await device.CreatePortMapAsync(mapping);
                
                // Try to retrieve confirmation on the port map we just created:
                try 
                {
                    Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, 6001);
                    Console.WriteLine("Specific Mapping: protocol={0}, public={1}, private={2}", m.Protocol, m.PublicPort,
                        m.PrivatePort);
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
     
                // Try retrieving all port maps:
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
                    Console.WriteLine("Couldn't get all mappings");
                }
     
                Console.WriteLine("External IP: {0}", await device.GetExternalIPAsync());
                Console.WriteLine("Done...");
            } 
            finally 
            {
                _locker.Release();
            }
        }
    }
}
