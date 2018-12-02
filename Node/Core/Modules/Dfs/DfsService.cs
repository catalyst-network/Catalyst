using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Dfs
{
//    public class DfsService : ServiceBase, IDfsService
//    {
//        private IPfs Dfs { get; set; }
//        private IDfsSettings DfsSettings { get; set; }
//        
//        public DfsService(IDfsSettings settings, IDfs Dfs)
//        {
//            
//        }
        
        /// <summary>
        ///   Start IPFS daemon and set client and settings to null
        /// </summary>
//        public void StartService()
//        {
//            if (_client != null)
//            {
//                TryToConnectClient(); // just to validate that connection with daemon is alive too
//                return;
//            }
//
//            _client  = new IpfsClient();
//            
//            _settings = settings;            
//            _defaultApiEndPoint = IpfsClient.DefaultApiUri + _settings.IpfsVersionApi;
//            
//            TryToConnectClient();
//        }
//        
//        /// <summary>
//        ///   Stop IPFS daemon and set client and settings to null
//        /// </summary>
//        public void StopService()
//        {
//            var localByName = Process.GetProcessesByName("ipfs");
//            if (localByName.Length == 1)
//            {
//                localByName[0].Kill(); // kill daemon process
//            }
//
//            if (_client != null)
//            {
//                if (IsClientConnected()) // if still connected then operation failed
//                {
//                    throw new InvalidOperationException();
//                }
//            }
//
//            _client = null;
//            _settings = null;
//        }
//
//    }
}