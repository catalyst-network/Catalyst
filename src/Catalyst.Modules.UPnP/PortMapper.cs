using System;
using System.Threading;
using Mono.Nat;

namespace Catalyst.Modules.UPnP
{
    class PortMapper{
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(100000);  
       
        public bool Run()
        {
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.DeviceLost += DeviceLost; 
            
            // If you know the gateway address, you can directly search for a device at that IP
            //NatUtility.Search (System.Net.IPAddress.Parse ("192.168.0.1"), NatProtocol.Pmp);
            //NatUtility.Search (System.Net.IPAddress.Parse ("192.168.0.1"), NatProtocol.Upnp);
            NatUtility.StartDiscovery();

            var endDate = DateTime.Now + Timeout;
            Console.WriteLine("Discovery started");
            while (DateTime.Now <= endDate)
            {
                Thread.Sleep(500000);
                NatUtility.StopDiscovery();
                NatUtility.StartDiscovery();
            }

            return true;
        }

        readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
     
        private async void DeviceFound(object sender, DeviceEventArgs args){ 
            await _locker.WaitAsync();
            try
            {
                INatDevice device = args.Device;
     
                // Only interact with one device at a time. Some devices support both
                // upnp and nat-pmp.
     
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Device found: {0}", device.NatProtocol);
                Console.ResetColor();
                Console.WriteLine("Type: {0}", device.GetType().Name);
     
                Console.WriteLine("IP: {0}", await device.GetExternalIPAsync());
     
                Console.WriteLine("---");

                // Try to create a new port map:
                var mapping = new Mapping(Protocol.Tcp, 6001, 6001);
                await device.CreatePortMapAsync(mapping);
                Console.WriteLine("Create Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort,
                    mapping.PrivatePort);
     
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
     
                // Try retrieving all port maps:
                try 
                {
                    var mappings = await device.GetAllMappingsAsync();
                    if (mappings.Length == 0)
                        Console.WriteLine("No existing uPnP mappings found.");
                    foreach (Mapping mp in mappings)
                        Console.WriteLine("Existing Mappings: protocol={0}, public={1}, private={2}", mp.Protocol, mp.PublicPort, mp.PrivatePort);
                } 
                catch
                {
                    Console.WriteLine("Couldn't get all mappings");
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
     
        private void DeviceLost(object sender, DeviceEventArgs args) {
            INatDevice device = args.Device;
     
            Console.WriteLine("Device Lost");
            Console.WriteLine("Type: {0}", device.GetType().Name);
        }
    }
}
