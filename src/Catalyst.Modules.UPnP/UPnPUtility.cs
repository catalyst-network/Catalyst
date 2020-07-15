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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mono.Nat;
using Serilog;

namespace Catalyst.Modules.UPnP
{
    public sealed class UPnPUtility : IUPnPUtility
    {
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);
        private readonly INatUtilityProvider _natUtilityProvider;
        private readonly ILogger _logger;

        public UPnPUtility(INatUtilityProvider natUtilityProvider, ILogger logger)
        {
            _natUtilityProvider = natUtilityProvider;
            _logger = logger;
        }
        
        public async Task<UPnPConstants.Result> MapPorts(Mapping[] ports, int timeoutInSeconds = UPnPConstants.DefaultTimeout, bool delete = false)
        {
            var tcs = new TaskCompletionSource<bool>();
            var remappingLogic = delete ? (Func<Mapping[], Mapping[], INatDevice, Task>)DeletePortMappingsIfExisting : AddMappingsIfNotPreExisting;
            await PerformEventOnDeviceFound(device => ReMapPorts(device, ports, remappingLogic, tcs), tcs, timeoutInSeconds).ConfigureAwait(false);
            return tcs.Task.IsCompletedSuccessfully ? UPnPConstants.Result.TaskFinished : UPnPConstants.Result.Timeout;
        }
        
        public async Task<IPAddress> GetPublicIpAddress(int timeoutInSeconds = UPnPConstants.DefaultTimeout)
        {
            var tcs = new TaskCompletionSource<IPAddress>(); 
            await PerformEventOnDeviceFound(device => GetExternalIpAddress(device, tcs), tcs, timeoutInSeconds).ConfigureAwait(false);
            return tcs.Task.IsCompletedSuccessfully ? tcs.Task.Result : null;
        }

        private async Task PerformEventOnDeviceFound<T>(Action<INatDevice> onDeviceFound, TaskCompletionSource<T> tcs, int timeoutInSeconds)
        {
            void DeviceFoundFunc(object o, DeviceEventArgs args) => onDeviceFound(args.Device);
            _natUtilityProvider.DeviceFound += DeviceFoundFunc;
            
            _natUtilityProvider.StartDiscovery();
            _logger.Information(UPnPConstants.StartedSearching);

            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds))).ConfigureAwait(false);


            _natUtilityProvider.DeviceFound -= DeviceFoundFunc;

            _natUtilityProvider.StopDiscovery();
            _logger.Information(UPnPConstants.StoppedSearching);
        }
        

        private async void ReMapPorts(INatDevice device, Mapping[] newMappings, Func<Mapping[], Mapping[], INatDevice, Task> remappingLogic, TaskCompletionSource<bool> tcs)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                var existingMappings = await device.GetAllMappingsAsync().ConfigureAwait(false);
                await remappingLogic(existingMappings, newMappings, device).ConfigureAwait(false);
                tcs.SetResult(true);
            }
            catch(Exception e)
            {
                _logger.Information(UPnPConstants.CouldNotCommunicateWithRouterException, e.ToString());
                tcs.SetException(e);
            }
            finally
            {
                _locker.Release();
            }
        }

        private async Task DeletePortMappingsIfExisting(Mapping[] existingMappings, Mapping[] newMappings, INatDevice device)
        {
            foreach (var newMapping in newMappings)
            {
                if (Array.Exists(existingMappings, m => m.Equals(newMapping)))
                {
                    var portDeleted = await device.DeletePortMapAsync(newMapping).ConfigureAwait(false);
                    _logger.Information(
                        portDeleted.Equals(newMapping)
                            ? UPnPConstants.DeletedMapping
                            : UPnPConstants.CouldNotDeleteMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                }
                else
                {
                    _logger.Information(UPnPConstants.NoExistingMapping
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
                    var portMapped = await device.CreatePortMapAsync(newMapping).ConfigureAwait(false);
                    if (portMapped.Equals(newMapping))
                    {
                        _logger.Information(UPnPConstants.CreatedMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                    else
                    {
                        await device.DeletePortMapAsync(portMapped);
                        _logger.Information(UPnPConstants.CouldNotCreateMapping, newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                    }
                }
                else
                {
                    _logger.Information(
                        UPnPConstants.ConflictingMappingExists,
                        newMapping.Protocol, newMapping.PublicPort, newMapping.PrivatePort);
                }
            }
        }
        
        private async void GetExternalIpAddress(INatDevice device, TaskCompletionSource<IPAddress> tcs)
        {
            await _locker.WaitAsync().ConfigureAwait(false);
            try
            {
                var ipAddress = await device.GetExternalIPAsync();
                tcs.SetResult(ipAddress);
                
            }
            catch(Exception e)
            {
                _logger.Information(UPnPConstants.CouldNotCommunicateWithRouterException, e.ToString());
                
                tcs.SetException(e);
            }
            finally
            {
                _locker.Release();
            }
        }
    }
    
}
