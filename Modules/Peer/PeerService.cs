using System;
using System.IO;
using System.Threading.Tasks;

namespace ADL.Peer
{
    /// <summary>
    /// The Peer Service 
    /// </summary>
    public class PeerService : IPeerService
    {
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        public static async Task StartService(IPeerSettings p2PSettings, DirectoryInfo dataDir)
        {
#if DEBUG
            Console.WriteLine("peer start service " + dataDir); 
            Console.WriteLine("start service param " + dataDir);            
#endif
            DataDir = dataDir;
            PeerSettings = p2PSettings;
            await AsyncWrapper();
        }

            
        public bool StopService()
        {
            return Peer.ShutDown();
        }
        
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static Task AsyncWrapper()
        {         
            var task = Task.Factory.StartNew(RunWatson);
            task.ConfigureAwait(false);
            return task;
        }

    }
}
