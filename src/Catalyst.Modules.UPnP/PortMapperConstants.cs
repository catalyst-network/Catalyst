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
