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

namespace Catalyst.Modules.UPnP
{
    public static class PortMapperConstants
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
        public const string DefaultUdpProperty = "CatalystNodeConfiguration.Rpc.Port";
        public const string DefaultTcpProperty = "CatalystNodeConfiguration.Peer.Port";
             
        public enum Result 
        {
            Timeout,
            TaskFinished,
        }

        public const int DefaultTimeout = 10;
    }
}
