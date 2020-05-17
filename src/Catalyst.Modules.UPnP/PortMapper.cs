#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

using System;
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

        public PortMapper(INatUtilityProvider natUtilityProvider, ILogger logger)
        {
            _natUtilityProvider = natUtilityProvider;
            _logger = logger;
        }

        private async Task<PortMapperConstants.Result> PerformEventOnDeviceFound(int timeoutInSeconds, CancellationToken token, Action<INatDevice> onDeviceFound)
        {
            void DeviceFoundFunc(object o, DeviceEventArgs args) => onDeviceFound(args.Device);
            _natUtilityProvider.DeviceFound += DeviceFoundFunc;
            
            _natUtilityProvider.StartDiscovery();
            _logger.Information(PortMapperConstants.StartedSearching);

            var result = await DelayUntilTimeoutOrMappingTaskEnds(timeoutInSeconds, token);

            _natUtilityProvider.DeviceFound -= DeviceFoundFunc;
            
            _natUtilityProvider.StopDiscovery();
            _logger.Information(PortMapperConstants.StoppedSearching);
            
            if(result==PortMapperConstants.Result.Timeout){_logger.Information(PortMapperConstants.CouldNotCommunicateWithRouter);}

            return result;
        }

        private static async Task<PortMapperConstants.Result> DelayUntilTimeoutOrMappingTaskEnds(int timeoutInSeconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds), token);
            }
            catch(TaskCanceledException)
            {
                return PortMapperConstants.Result.TaskFinished;
            }

            return PortMapperConstants.Result.Timeout;
        }

        public Task<PortMapperConstants.Result> MapPorts(Mapping[] ports,
            int timeoutInSeconds = PortMapperConstants.DefaultTimeout, bool delete = false)
        {
            var tokenSource = new CancellationTokenSource();
            
            var remappingLogic = delete ? (Func<Mapping[], Mapping[], INatDevice, Task>)DeletePortMappingsIfExisting : AddMappingsIfNotPreExisting;
            return PerformEventOnDeviceFound(timeoutInSeconds, tokenSource.Token, device => ReMapPorts(device, ports, tokenSource, remappingLogic));
        }
        
        private async void ReMapPorts(INatDevice device, Mapping[] newMappings, CancellationTokenSource tokenSource, Func<Mapping[], Mapping[], INatDevice, Task> remappingLogic)
        {
            await _locker.WaitAsync();
            try
            {
                var existingMappings = await device.GetAllMappingsAsync();
                await remappingLogic(existingMappings, newMappings, device);
            }
            catch
            {
                _logger.Information(PortMapperConstants.CouldNotCommunicateWithRouter);
            }
            finally
            {
                tokenSource.Cancel();
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
    }
    
}
