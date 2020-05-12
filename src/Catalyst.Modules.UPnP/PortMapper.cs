using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Serilog;

namespace Catalyst.Modules.UPnP
{
    
    public sealed class PortMapper
    {
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly INatUtilityProvider _natUtilityProvider;
        private readonly ILogger _logger;
        private bool _isTaskFinished;
        public PortMapper(INatUtilityProvider natUtilityProvider, ILogger logger)
        {
            _natUtilityProvider = natUtilityProvider;
            _logger = logger;
        }

        private async Task<int> PerformEventOnDeviceFound(int timeoutInSeconds, EventHandler<DeviceEventArgs> onDeviceFound)
        {
            _isTaskFinished = false;
            _natUtilityProvider.DeviceFound += onDeviceFound;
            
            _natUtilityProvider.StartDiscovery();
            _logger.Information(PortMapperConstants.StartedSearching);
            
            var stop = DateTime.Now.AddSeconds(timeoutInSeconds);
            while (DateTime.Now < stop && !_isTaskFinished)
            {
                await Task.Delay(10);
            }
            
            _natUtilityProvider.StopDiscovery();
            _natUtilityProvider.DeviceFound -= onDeviceFound;
            
            _logger.Information(PortMapperConstants.StoppedSearching);
            if(!_isTaskFinished){_logger.Information(PortMapperConstants.CouldNotCommunicateWithRouter);}

            return 0;
        }

        public async Task<int> AddPortMappings(Mapping[] ports, int timeoutInSeconds = 30)
        {
            return await PerformEventOnDeviceFound(timeoutInSeconds, (sender, args) => ReMapPorts(args, ports, AddMappingsIfNotPreExisting));
        }
        
        public async Task<int> DeletePortMappings(Mapping[] ports, int timeoutInSeconds = 30)
        {
            return await PerformEventOnDeviceFound(timeoutInSeconds, (sender, args) => ReMapPorts(args, ports, DeletePortMappingsIfExisting));
        }
        
        private async void ReMapPorts(DeviceEventArgs args, Mapping[] portMappings, Func<Mapping[], Mapping[], INatDevice, Task> remappingLogic)
        {
            await _locker.WaitAsync();
            try
            {
                var device = args.Device;

                var existingMappings = await device.GetAllMappingsAsync();
                await remappingLogic(existingMappings, portMappings, device);
            }
            catch
            {
                _logger.Information(PortMapperConstants.CouldNotCommunicateWithRouter);
            }
            finally
            {
                _isTaskFinished = true;
                _locker.Release();
            }
        }

        private async Task DeletePortMappingsIfExisting(Mapping[] existingMappings, Mapping[] newMappings, INatDevice device)
        {
            foreach (var newMapping in newMappings)
            {
                if (Array.Exists(existingMappings, m => m.Equals(newMapping)))
                {
                    var portDeleted = await device.DeletePortMapAsync(newMapping);
                    _logger.Information(
                        portDeleted.Equals(newMapping)
                            ? PortMapperConstants.DeletedMapping
                            : PortMapperConstants.CouldNotDeleteMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                }
                else
                {
                    _logger.Information(PortMapperConstants.NoExistingMapping
                        ,
                        newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                }
            }
        }

        private async Task AddMappingsIfNotPreExisting(Mapping[] existingMappings, Mapping[] newMappings, INatDevice device)
        {
            foreach (var newMapping in newMappings)
            {
                if (!existingMappings.Any(m =>  m.PrivatePort==newMapping.PrivatePort || m.PublicPort==newMapping.PublicPort))
                {
                    var portMapped = await device.CreatePortMapAsync(newMapping);
                    if (portMapped.Equals(newMapping))
                    {
                        _logger.Information(PortMapperConstants.CreatedMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                    else
                    {
                        await device.DeletePortMapAsync(portMapped);
                        _logger.Information(PortMapperConstants.CouldNotCreateMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                }
                else
                {
                    _logger.Information(
                        PortMapperConstants.ConflictingMappingExists,
                        newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                }
            }
        }

        private static class PortMapperConstants
        {
             public const string CouldNotCommunicateWithRouter = "Sorry, it wasn't possible to communicate with your router.";
             public const string NoExistingMapping = "There is no existing mapping for protocol = {0}, public port = {1}, private port = {2}.";
             public const string DeletedMapping =
                 "Deleted the mapping for protocol = {0}, public port = {1}, private port = {2}.";
             public const string CouldNotDeleteMapping =
                 "It wasn't possible to delete the mapping for protocol = {0}, public port = {1}, private port = {2}.";
             public const string CouldNotCreateMapping =
                 "It wasn't possible to create a mapping for protocol = {0}, public port = {1}, private port = {2}.";
             public const string CreatedMapping =
                 "Created a mapping for protocol = {0}, public port = {1}, private port = {2}.";
             public const string ConflictingMappingExists =
                 "There is an existing mapping which conflicts with requested mapping protocol = {0}, public port = {1}, private port = {2}.";
             public const string StoppedSearching = "Stopped searching for the router.";
             public const string StartedSearching = "Started searching for a compatible router...";
        }
    }
    
}
