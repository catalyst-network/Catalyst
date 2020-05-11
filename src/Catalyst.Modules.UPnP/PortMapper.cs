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
        private readonly INatUtilityProvider _natUtilityProvider;
        private readonly ILogger _logger;
        private bool _isTaskFinished;
        public PortMapper(INatUtilityProvider natUtilityProvider, ILogger logger)
        {
            _natUtilityProvider = natUtilityProvider;
            _logger = logger;
        }

        private async Task PerformEventOnDeviceFound(int timeoutInSeconds, EventHandler<DeviceEventArgs> onDeviceFound)
        {
            _isTaskFinished = false;
            _natUtilityProvider.DeviceFound += onDeviceFound;
            
            _natUtilityProvider.StartDiscovery();
            _logger.Information("Started searching for a compatible router...");
            
            var stop = DateTime.Now.AddSeconds(timeoutInSeconds);
            while (DateTime.Now < stop && !_isTaskFinished)
            {
                await Task.Delay(10);
            }
            
            _natUtilityProvider.StopDiscovery();
            _natUtilityProvider.DeviceFound -= onDeviceFound;
            _logger.Information("Stopped searching for the router.");
            if(!_isTaskFinished){_logger.Information("A compatible router could not be found.");}
        }
   
        public async Task AddPortMappings(IEnumerable<Mapping> ports, int timeoutInSeconds = 30)
        {
            await PerformEventOnDeviceFound(timeoutInSeconds, (sender, args) => AddPortMappings(args, ports));
        }
        
        public async Task DeletePortMappings(IEnumerable<Mapping> ports, int timeoutInSeconds = 30)
        {
            await PerformEventOnDeviceFound(timeoutInSeconds, (sender, args) => DeletePortMappings(args, ports));
        }

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private async void AddPortMappings(DeviceEventArgs args, IEnumerable<Mapping> portMappings)
        {
            await _locker.WaitAsync();
            try
            {
                var device = args.Device;

                var existingMappings = await device.GetAllMappingsAsync();

                foreach (var newMapping in portMappings)
                {
                    if (CanCreateNewMapping(existingMappings, newMapping))
                    {
                        var portMapped = await device.CreatePortMapAsync(newMapping);
                        _logger.Information(
                            portMapped.Equals(newMapping)
                                ? PortMapperConstants.CreatedMapping
                                : PortMapperConstants.CouldNotCreateMapping,
                            newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                    else
                    {
                        _logger.Information(
                            PortMapperConstants.ConflictingMappingExists,
                            newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                }
            }
            catch
            {
                _logger.Information(PortMapperConstants.CouldNotFindRouter);
            }
            finally
            {
                _isTaskFinished = true;
                _locker.Release();
            }
        }

        private async void DeletePortMappings(DeviceEventArgs args,
                IEnumerable<Mapping> portMappings)
        {
            await _locker.WaitAsync();
            try
            {
                var device = args.Device;

                var mappings = await device.GetAllMappingsAsync();

                foreach (var p in portMappings)
                {
                    if (Array.Exists(mappings, m => m.Equals(p)))
                    {
                        var portDeleted = await device.DeletePortMapAsync(p);
                        _logger.Information(
                            portDeleted.Equals(p)
                                ? PortMapperConstants.DeletedMapping
                                : PortMapperConstants.CouldNotDeleteMapping, p.Protocol, p.PublicPort, p.PrivatePort);
                    }
                    else
                    {
                        _logger.Information(PortMapperConstants.NoExistingMapping
                            ,
                            p.Protocol, p.PublicPort, p.PrivatePort);
                    }
                }
            }
            catch
            {
                _logger.Information(PortMapperConstants.CouldNotFindRouter);
            }
            finally
            {
                _isTaskFinished = true;
                _locker.Release();
            }
        }

        private static bool CanCreateNewMapping(Mapping[] existingMappings, Mapping newMapping)
        {
            return !Array.Exists(existingMappings, m =>  m.PrivatePort==newMapping.PrivatePort || m.PublicPort==newMapping.PublicPort);
        }

        private static class PortMapperConstants
        {
             public const string CouldNotFindRouter = "Sorry, it wasn't possible to communicate with your router.";
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
        }

    }
    
}
