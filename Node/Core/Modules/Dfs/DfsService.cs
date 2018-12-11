using ADL.Ipfs;
using ADL.Node.Core.Helpers.Services;

namespace ADL.Node.Core.Modules.Dfs
{
    public class DfsService : ServiceBase, IDfsService
    {
        private IIpfs Dfs { get; set; }
        private IDfsSettings DfsSettings { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dfs"></param>
        /// <param name="settings"></param>
        public DfsService(IIpfs dfs, IDfsSettings settings)
        {
            Dfs = dfs;
            DfsSettings = settings;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StartService()
        {
            Dfs.CreateIpfsClient(DfsSettings.IpfsVersionApi, DfsSettings.ConnectRetries);
            return true;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool StopService()
        {
            Dfs.DestroyIpfsClient();
            return true;
        }

        /// <summary>
        /// Get current implementation of this service
        /// </summary>
        /// <returns></returns>
        public IIpfs GetImpl()
        {
            return Dfs;
        }
    }
}